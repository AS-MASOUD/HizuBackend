using Hizu.Application.Common.Models;
using Hizu.Application.Features.Admin.Commands;
using Hizu.Application.Features.Admin.DTOs;
using Hizu.Application.Features.Admin.Queries;
using Hizu.Application.Features.Orders.Commands;
using Hizu.Application.Features.Orders.DTOs;
using Hizu.Application.Features.Orders.Queries;
using Hizu.Application.Features.SpecialOrders.Commands;
using Hizu.Application.Features.SpecialOrders.DTOs;
using Hizu.Application.Features.SpecialOrders.Queries;
using Hizu.Domain.Constants;
using Hizu.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

[Authorize(Roles = AppRoles.Admin)]
public class AdminController : ApiControllerBase
{
    [HttpGet("stats")]
    public async Task<ActionResult<AdminDashboardStatsDto>> GetStats()
    {
        var result = await Mediator.Send(new GetAdminDashboardStatsQuery());
        return Ok(result);
    }

    [HttpGet("orders")]
    public async Task<ActionResult<PagedResult<OrderDto>>> GetOrders([FromQuery] GetAllOrdersQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpPatch("orders/{id:guid}/status")]
    public async Task<ActionResult> UpdateOrderStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
    {
        await Mediator.Send(new UpdateOrderStatusCommand(id, request.Status));
        return NoContent();
    }

    [HttpGet("special-orders")]
    public async Task<ActionResult<PagedResult<SpecialOrderDto>>> GetSpecialOrders([FromQuery] GetAllSpecialOrdersQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpPost("special-orders/{id:guid}/reply")]
    public async Task<ActionResult> ReplySpecialOrder(Guid id, [FromBody] ReplySpecialOrderRequest request)
    {
        await Mediator.Send(new ReplySpecialOrderCommand(id, request.Reply, request.QuotedPrice, request.NewStatus));
        return NoContent();
    }

    [HttpPatch("special-orders/{id:guid}/status")]
    public async Task<ActionResult> UpdateSpecialOrderStatus(Guid id, [FromBody] UpdateSpecialOrderStatusRequest request)
    {
        await Mediator.Send(new UpdateSpecialOrderStatusCommand(id, request.Status));
        return NoContent();
    }

    [HttpGet("users")]
    public async Task<ActionResult<PagedResult<AdminUserDto>>> GetUsers([FromQuery] GetAdminUsersQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpPut("users/{id:guid}/roles")]
    public async Task<ActionResult> UpdateUserRoles(Guid id, [FromBody] UpdateRolesRequest request)
    {
        await Mediator.Send(new UpdateUserRolesCommand(id, request.Roles));
        return NoContent();
    }
}

public record UpdateOrderStatusRequest(OrderStatus Status);
public record ReplySpecialOrderRequest(string Reply, decimal? QuotedPrice, SpecialOrderStatus? NewStatus);
public record UpdateSpecialOrderStatusRequest(SpecialOrderStatus Status);
public record UpdateRolesRequest(List<string> Roles);
