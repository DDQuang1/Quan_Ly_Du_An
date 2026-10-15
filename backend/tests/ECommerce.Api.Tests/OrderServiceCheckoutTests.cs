using ECommerce.Api.Data;
using ECommerce.Api.DTOs.Orders;
using ECommerce.Api.Entities;
using ECommerce.Api.Exceptions;
using ECommerce.Api.Services.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ECommerce.Api.Tests;

public sealed class OrderServiceCheckoutTests
{
    [Fact]
    public async Task CheckoutSnapshotsServerDataTotalsAndClearsCart()
    {
        await using var f = await Fixture.CreateAsync();
        var result = await f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = 11, PaymentMethod = "COD", Note = "  call first  " });
        Assert.Equal(25m, result.TotalAmount);
        Assert.Equal("PENDING", result.PaymentStatus);
        Assert.Equal("PENDING", result.OrderStatus);
        Assert.Equal("call first", result.Note);
        Assert.Equal([20m, 5m], result.Items.Select(x => x.TotalPrice));
        Assert.Empty(await f.Context.CartItems.ToListAsync());
        Assert.Equal(3, await f.Context.Products.SumAsync(x => x.StockQuantity));
    }

    [Fact]
    public async Task MockQrCheckoutCreatesPendingOrderAndClearsCart()
    {
        await using var f = await Fixture.CreateAsync();

        var result = await f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = 11, PaymentMethod = "QR" });

        Assert.Equal("QR", result.PaymentMethod);
        Assert.Equal("PENDING", result.PaymentStatus);
        Assert.Equal(25m, result.TotalAmount);
        Assert.Empty(await f.Context.CartItems.ToListAsync());
    }

    [Fact]
    public async Task MockPaymentUsesSavedOrderAmountAndConfirmationMarksOnlyPaymentPaid()
    {
        await using var f = await Fixture.CreateAsync();
        var order = await f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = 11, PaymentMethod = "QR" });

        var payment = await f.Service.GetMockPaymentAsync(7, order.OrderID);
        Assert.Equal(25m, payment.Amount);
        Assert.Equal($"ELECTROTECH DH{order.OrderID}", payment.TransferContent);
        Assert.Equal("PENDING", payment.PaymentStatus);

        var confirmed = await f.Service.ConfirmMockPaymentAsync(7, order.OrderID);
        Assert.Equal("PAID", confirmed.PaymentStatus);
        Assert.Equal("PAID", (await f.Service.GetAsync(7, order.OrderID)).PaymentStatus);
        Assert.Equal("PENDING", (await f.Service.GetAsync(7, order.OrderID)).OrderStatus);
        Assert.Equal("PAID", (await f.Service.ConfirmMockPaymentAsync(7, order.OrderID)).PaymentStatus);
        Assert.Single(await f.Context.Orders.ToListAsync());
    }

    [Fact]
    public async Task MockPaymentHidesForeignOrdersAndRejectsCodConfirmation()
    {
        await using var f = await Fixture.CreateAsync();
        var order = await f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = 11, PaymentMethod = "COD" });

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => f.Service.GetMockPaymentAsync(8, order.OrderID));
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => f.Service.ConfirmMockPaymentAsync(8, order.OrderID));
        await Assert.ThrowsAsync<DomainConflictException>(() => f.Service.ConfirmMockPaymentAsync(7, order.OrderID));
        Assert.Equal("PENDING", (await f.Service.GetAsync(7, order.OrderID)).PaymentStatus);
    }

    [Theory]
    [InlineData(0, "COD")]
    [InlineData(11, "CARD")]
    public async Task CheckoutRejectsInvalidRequest(int addressId, string paymentMethod)
    {
        await using var f = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<DomainValidationException>(() => f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = addressId, PaymentMethod = paymentMethod }));
        Assert.Equal(2, await f.Context.CartItems.CountAsync());
        Assert.Empty(await f.Context.Orders.ToListAsync());
    }

    [Fact]
    public async Task CheckoutRejectsForeignAddressEmptyCartAndInsufficientStock()
    {
        await using var f = await Fixture.CreateAsync();
        await Assert.ThrowsAsync<ResourceNotFoundException>(() => f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = 12 }));
        (await f.Context.Products.FindAsync(101))!.StockQuantity = 1;
        await f.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainValidationException>(() => f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = 11 }));
        f.Context.CartItems.RemoveRange(f.Context.CartItems);
        await f.Context.SaveChangesAsync();
        await Assert.ThrowsAsync<DomainValidationException>(() => f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = 11 }));
    }

    [Fact]
    public async Task SaveFailureRollsBackOrderAndRetainsCart()
    {
        await using var f = await Fixture.CreateAsync(failAfterSave: true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = 11 }));

        f.Context.ChangeTracker.Clear();
        Assert.Equal(2, await f.Context.CartItems.CountAsync());
        Assert.Empty(await f.Context.Orders.ToListAsync());
    }

    [Fact]
    public async Task CancelOrderRestoresStockAndBlocksDuplicateCancellation()
    {
        await using var f = await Fixture.CreateAsync();
        var order = await f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = 11, PaymentMethod = "COD" });

        var cancelled = await f.Service.CancelOrderAsync(7, order.OrderID);
        Assert.Equal("CANCELLED", cancelled.OrderStatus);

        var dbOrder = await f.Context.Orders.FindAsync(order.OrderID);
        Assert.NotNull(dbOrder);
        Assert.Equal("CANCELLED", dbOrder.OrderStatus);
        Assert.NotNull(dbOrder.CancelledAt);

        // Verify duplicate cancellation throws DomainConflictException
        await Assert.ThrowsAsync<DomainConflictException>(() => f.Service.CancelOrderAsync(7, order.OrderID));

        // Verify transactions were recorded
        var transactions = await f.Context.InventoryTransactions.Where(t => t.TransactionType == "CANCEL_ORDER").ToListAsync();
        Assert.Equal(2, transactions.Count);
    }

    [Fact]
    public async Task CancelForeignOrderThrowsResourceNotFound()
    {
        await using var f = await Fixture.CreateAsync();
        var order = await f.Service.CheckoutAsync(7, new CheckoutDto { AddressID = 11, PaymentMethod = "COD" });

        await Assert.ThrowsAsync<ResourceNotFoundException>(() => f.Service.CancelOrderAsync(8, order.OrderID));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        public AppDbContext Context { get; }
        public OrderService Service { get; }

        private Fixture(AppDbContext context, SqliteConnection connection)
        {
            Context = context;
            this.connection = connection;
            Service = new OrderService(context);
        }

        public static async Task<Fixture> CreateAsync(bool failAfterSave = false)
        {
            var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
            await connection.OpenAsync();
            var interceptor = new FailAfterSaveInterceptor();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .AddInterceptors(interceptor)
                .Options;
            var db = new AppDbContext(options);
            await db.Database.ExecuteSqlRawAsync("""
                CREATE TABLE Addresses (
                    AddressID INTEGER PRIMARY KEY, UserID INTEGER NOT NULL, ReceiverName TEXT NOT NULL,
                    ReceiverPhone TEXT NOT NULL, Province TEXT NULL, District TEXT NULL, Ward TEXT NULL,
                    FullAddress TEXT NOT NULL, IsDefault INTEGER NOT NULL DEFAULT 0, CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    UpdatedAt TEXT NULL);
                CREATE TABLE Products (
                    ProductID INTEGER PRIMARY KEY, CategoryID INTEGER NOT NULL, BrandID INTEGER NOT NULL,
                    SKU TEXT NOT NULL, ProductName TEXT NOT NULL, Description TEXT NULL, Specifications TEXT NULL,
                    Price NUMERIC NOT NULL, StockQuantity INTEGER NOT NULL, IsActive INTEGER NOT NULL DEFAULT 1,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, UpdatedAt TEXT NULL);
                CREATE TABLE Carts (
                    CartID INTEGER PRIMARY KEY, UserID INTEGER NOT NULL, CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, UpdatedAt TEXT NULL);
                CREATE TABLE CartItems (
                    CartItemID INTEGER PRIMARY KEY, CartID INTEGER NOT NULL, ProductID INTEGER NOT NULL,
                    Quantity INTEGER NOT NULL, AddedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, UpdatedAt TEXT NULL);
                CREATE TABLE Orders (
                    OrderID INTEGER PRIMARY KEY AUTOINCREMENT, UserID INTEGER NOT NULL, ReceiverName TEXT NOT NULL,
                    ReceiverPhone TEXT NOT NULL, Province TEXT NULL, District TEXT NULL, Ward TEXT NULL,
                    ShippingAddress TEXT NOT NULL, SubTotal NUMERIC NOT NULL, ShippingFee NUMERIC NOT NULL DEFAULT 0,
                    TotalAmount NUMERIC NOT NULL, PaymentMethod TEXT NOT NULL DEFAULT 'COD', PaymentStatus TEXT NOT NULL DEFAULT 'PENDING',
                    OrderStatus TEXT NOT NULL DEFAULT 'PENDING', Note TEXT NULL, CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP, UpdatedAt TEXT NULL,
                    ConfirmedAt TEXT NULL, CompletedAt TEXT NULL, CancelledAt TEXT NULL);
                CREATE TABLE OrderDetails (
                    OrderDetailID INTEGER PRIMARY KEY AUTOINCREMENT, OrderID INTEGER NOT NULL, ProductID INTEGER NOT NULL,
                    ProductName TEXT NOT NULL, SKU TEXT NOT NULL, Quantity INTEGER NOT NULL, UnitPrice NUMERIC NOT NULL,
                    TotalPrice NUMERIC GENERATED ALWAYS AS (Quantity * UnitPrice) STORED);
                CREATE TABLE InventoryTransactions (
                    InventoryTransactionID INTEGER PRIMARY KEY AUTOINCREMENT, ProductID INTEGER NOT NULL,
                    TransactionType TEXT NOT NULL, Quantity INTEGER NOT NULL, PreviousStock INTEGER NOT NULL,
                    NewStock INTEGER NOT NULL, Note TEXT NULL, CreatedBy INTEGER NULL,
                    CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP);
                """);
            db.Addresses.AddRange(
                new Address { AddressID = 11, UserID = 7, ReceiverName = "Receiver", ReceiverPhone = "0900", FullAddress = "1 Main" },
                new Address { AddressID = 12, UserID = 8, ReceiverName = "Other", ReceiverPhone = "0901", FullAddress = "2 Main" });
            db.Products.AddRange(Product(101, "A", "Product A", 10m, 2), Product(102, "B", "Product B", 5m, 1));
            db.Carts.Add(new Cart { CartID = 21, UserID = 7 });
            db.CartItems.AddRange(
                new CartItem { CartItemID = 31, CartID = 21, ProductID = 101, Quantity = 2 },
                new CartItem { CartItemID = 32, CartID = 21, ProductID = 102, Quantity = 1 });
            await db.SaveChangesAsync();
            interceptor.Enabled = failAfterSave;
            return new Fixture(db, connection);
        }
        private static Product Product(int id, string sku, string name, decimal price, int stock) => new()
        { ProductID = id, CategoryID = 1, BrandID = 1, SKU = sku, ProductName = name, Price = price, StockQuantity = stock, IsActive = true };
        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }

    private sealed class FailAfterSaveInterceptor : SaveChangesInterceptor
    {
        public bool Enabled { get; set; }

        public override ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            if (Enabled)
            {
                throw new InvalidOperationException("Injected post-save failure.");
            }

            return base.SavedChangesAsync(eventData, result, cancellationToken);
        }
    }
}
