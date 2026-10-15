using ECommerce.Api.DTOs;
using ECommerce.Api.DTOs.Orders;
using ECommerce.Api.Extensions;
using ECommerce.Api.Services.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Controllers;

[ApiController]
[Authorize(Roles = "Customer")]
[Route("api/orders")]
public sealed class OrdersController(IOrderService service) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDetailDto>> Checkout(CheckoutDto dto, CancellationToken ct)
    {
        var order = await service.CheckoutAsync(User.GetRequiredUserId(), dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = order.OrderID }, order);
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<OrderSummaryDto>>> List(
        [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default) =>
        Ok(await service.ListAsync(User.GetRequiredUserId(), pageNumber, pageSize, ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<OrderDetailDto>> GetById(long id, CancellationToken ct) =>
        Ok(await service.GetAsync(User.GetRequiredUserId(), id, ct));

    [HttpGet("{id:long}/mock-payment")]
    public async Task<ActionResult<MockPaymentDto>> GetMockPayment(long id, CancellationToken ct) =>
        Ok(await service.GetMockPaymentAsync(User.GetRequiredUserId(), id, ct));

    [HttpPost("{id:long}/mock-payment/confirm")]
    public async Task<ActionResult<MockPaymentDto>> ConfirmMockPayment(long id, CancellationToken ct) =>
        Ok(await service.ConfirmMockPaymentAsync(User.GetRequiredUserId(), id, ct));

    [HttpPost("{id:long}/cancel")]
    public async Task<ActionResult<OrderDetailDto>> Cancel(long id, CancellationToken ct) =>
        Ok(await service.CancelOrderAsync(User.GetRequiredUserId(), id, ct));
}
