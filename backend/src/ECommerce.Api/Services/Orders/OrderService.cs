using ECommerce.Api.Data;
using ECommerce.Api.Domain;
using ECommerce.Api.DTOs;
using ECommerce.Api.DTOs.Orders;
using ECommerce.Api.Entities;
using ECommerce.Api.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Services.Orders;

public sealed class OrderService(AppDbContext context) : IOrderService
{
    public async Task<OrderDetailDto> CheckoutAsync(int userId, CheckoutDto dto, CancellationToken ct = default)
    {
        if (dto.AddressID <= 0 ||
            (dto.PaymentMethod != OrderConstants.Cod && dto.PaymentMethod != OrderConstants.Qr))
        {
            throw new DomainValidationException();
        }

        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var address = await context.Addresses
            .SingleOrDefaultAsync(x => x.AddressID == dto.AddressID && x.UserID == userId, ct)
            ?? throw new ResourceNotFoundException();
        var cart = await context.Carts
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .SingleOrDefaultAsync(x => x.UserID == userId, ct);

        if (cart is null || cart.Items.Count == 0)
        {
            throw new DomainValidationException();
        }

        foreach (var line in cart.Items)
        {
            if (line.Quantity <= 0 || line.Product is null || !line.Product.IsActive || line.Quantity > line.Product.StockQuantity)
            {
                throw new DomainValidationException();
            }
        }

        var items = cart.Items
            .OrderBy(x => x.CartItemID)
            .Select(x => new OrderDetail
            {
                ProductID = x.ProductID,
                ProductName = x.Product.ProductName,
                SKU = x.Product.SKU,
                Quantity = x.Quantity,
                UnitPrice = x.Product.Price,
                TotalPrice = x.Product.Price * x.Quantity
            })
            .ToList();
        var subTotal = items.Sum(x => x.TotalPrice);
        var order = new Order
        {
            UserID = userId,
            ReceiverName = address.ReceiverName,
            ReceiverPhone = address.ReceiverPhone,
            Province = address.Province,
            District = address.District,
            Ward = address.Ward,
            ShippingAddress = address.FullAddress,
            SubTotal = subTotal,
            ShippingFee = 0m,
            TotalAmount = subTotal,
            PaymentMethod = dto.PaymentMethod,
            PaymentStatus = OrderConstants.Pending,
            OrderStatus = OrderConstants.Pending,
            Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
            OrderDetails = items
        };

        context.Orders.Add(order);
        context.CartItems.RemoveRange(cart.Items);
        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return MapDetail(order);
    }

    public async Task<PagedResult<OrderSummaryDto>> ListAsync(
        int userId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        if (pageNumber < 1 || pageSize is < 1 or > 100)
        {
            throw new DomainValidationException();
        }

        var query = context.Orders.AsNoTracking().Where(x => x.UserID == userId);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.OrderID)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new OrderSummaryDto(
                x.OrderID,
                x.TotalAmount,
                x.PaymentMethod,
                x.PaymentStatus,
                x.OrderStatus,
                x.CreatedAt,
                x.OrderDetails.Sum(item => item.Quantity)))
            .ToListAsync(ct);

        return new PagedResult<OrderSummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<OrderDetailDto> GetAsync(int userId, long orderId, CancellationToken ct = default)
    {
        var order = await context.Orders.AsNoTracking()
            .Include(x => x.OrderDetails)
            .SingleOrDefaultAsync(x => x.OrderID == orderId && x.UserID == userId, ct)
            ?? throw new ResourceNotFoundException();
        return MapDetail(order);
    }

    public async Task<MockPaymentDto> GetMockPaymentAsync(int userId, long orderId, CancellationToken ct = default)
    {
        var order = await context.Orders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OrderID == orderId && x.UserID == userId, ct)
            ?? throw new ResourceNotFoundException();
        EnsureQrOrder(order);
        return MapMockPayment(order);
    }

    public async Task<MockPaymentDto> ConfirmMockPaymentAsync(int userId, long orderId, CancellationToken ct = default)
    {
        var order = await context.Orders
            .SingleOrDefaultAsync(x => x.OrderID == orderId && x.UserID == userId, ct)
            ?? throw new ResourceNotFoundException();
        EnsureQrOrder(order);

        if (order.OrderStatus == OrderConstants.Cancelled ||
            (order.PaymentStatus != OrderConstants.Pending && order.PaymentStatus != OrderConstants.Paid))
        {
            throw new DomainConflictException();
        }

        if (order.PaymentStatus == OrderConstants.Pending)
        {
            order.PaymentStatus = OrderConstants.Paid;
            await context.SaveChangesAsync(ct);
        }

        return MapMockPayment(order);
    }

    public async Task<OrderDetailDto> CancelOrderAsync(int userId, long orderId, CancellationToken ct = default)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(ct);
        var order = await context.Orders
            .Include(x => x.OrderDetails)
            .SingleOrDefaultAsync(x => x.OrderID == orderId && x.UserID == userId, ct)
            ?? throw new ResourceNotFoundException();

        if (order.OrderStatus == OrderConstants.Cancelled ||
            order.OrderStatus is "COMPLETED" or "SHIPPING")
        {
            throw new DomainConflictException();
        }

        order.OrderStatus = OrderConstants.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.UpdatedAt = DateTime.UtcNow;

        foreach (var detail in order.OrderDetails)
        {
            var product = await context.Products.SingleOrDefaultAsync(p => p.ProductID == detail.ProductID, ct);
            if (product != null)
            {
                var prevStock = product.StockQuantity;
                product.StockQuantity += detail.Quantity;
                product.UpdatedAt = DateTime.UtcNow;

                context.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductID = detail.ProductID,
                    TransactionType = "CANCEL_ORDER",
                    Quantity = detail.Quantity,
                    PreviousStock = prevStock,
                    NewStock = product.StockQuantity,
                    Note = $"Cancel order #{order.OrderID}",
                    CreatedBy = userId,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return MapDetail(order);
    }

    private static void EnsureQrOrder(Order order)
    {
        if (order.PaymentMethod != OrderConstants.Qr)
        {
            throw new DomainConflictException();
        }
    }

    private static MockPaymentDto MapMockPayment(Order order) => new(
        order.OrderID,
        order.TotalAmount,
        "VND",
        $"ELECTROTECH DH{order.OrderID}",
        "ElectroTech Demo Bank",
        "0000 0000 0000",
        "ELECTROTECH DEMO",
        order.PaymentStatus);

    private static OrderDetailDto MapDetail(Order order) => new(
        order.OrderID, order.UserID, order.ReceiverName, order.ReceiverPhone,
        order.Province, order.District, order.Ward, order.ShippingAddress,
        order.SubTotal, order.ShippingFee, order.TotalAmount, order.PaymentMethod,
        order.PaymentStatus, order.OrderStatus, order.Note, order.CreatedAt,
        order.OrderDetails.OrderBy(x => x.OrderDetailID)
            .Select(x => new OrderItemDto(x.ProductID, x.ProductName, x.SKU, x.Quantity, x.UnitPrice, x.TotalPrice))
            .ToList());
}
