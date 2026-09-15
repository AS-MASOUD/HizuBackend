using Hizu.Application.Features.Orders.Commands;
using Hizu.Application.Features.Orders.DTOs;
using Hizu.Application.Features.Orders.Queries;
using Hizu.Domain.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

[Authorize]
public class OrdersController : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<OrderDto>> CreateOrder([FromBody] CheckoutRequest request)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var command = new CreateOrderCommand
        {
            UserId = CurrentUserId.Value,
            ShippingAddress = request.ShippingAddress
        };

        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet]
    public async Task<ActionResult<List<OrderSummaryDto>>> GetMyOrders()
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        var result = await Mediator.Send(new GetUserOrdersQuery(CurrentUserId.Value));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<OrderDto>> GetById(Guid id)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        var result = await Mediator.Send(new GetOrderByIdQuery(id, CurrentUserId.Value));
        return Ok(result);
    }
}

public record CheckoutRequest(Address ShippingAddress);
