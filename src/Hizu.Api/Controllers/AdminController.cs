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
using Hizu.Application.Features.Products.Commands;
using Hizu.Application.Features.Products.DTOs;
using Hizu.Application.Features.Products.Queries;
using Hizu.Domain.Constants;
using Hizu.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

[Authorize(Roles = AppRoles.AdminOrSysManager)]
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

    [Authorize(Roles = AppRoles.SysManager)]
    [HttpPut("users/{id:guid}/roles")]
    public async Task<ActionResult> UpdateUserRoles(Guid id, [FromBody] UpdateRolesRequest request)
    {
        await Mediator.Send(new UpdateUserRolesCommand(id, request.Roles));
        return NoContent();
    }

    [HttpGet("comments")]
    public async Task<ActionResult<PagedResult<ProductCommentDto>>> GetComments(
        [FromQuery] bool? isApproved = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var result = await Mediator.Send(new GetAdminCommentsQuery(isApproved, page, pageSize));
        return Ok(result);
    }

    [HttpPatch("comments/{id:guid}/approval")]
    public async Task<ActionResult> ToggleCommentApproval(Guid id, [FromBody] ToggleApprovalRequest request)
    {
        await Mediator.Send(new ApproveProductCommentCommand(id, request.IsApproved));
        return NoContent();
    }

    [HttpPost("comments/{id:guid}/reply")]
    public async Task<ActionResult<ProductCommentDto>> ReplyComment(Guid id, [FromBody] ReplyCommentRequest request)
    {
        var result = await Mediator.Send(new ReplyProductCommentCommand(id, request.ReplyText, CurrentUserId));
        return Ok(result);
    }

    [HttpDelete("comments/{id:guid}")]
    public async Task<ActionResult> DeleteComment(Guid id)
    {
        await Mediator.Send(new DeleteProductCommentCommand(id));
        return NoContent();
    }
}

public record UpdateOrderStatusRequest(OrderStatus Status);
public record ReplySpecialOrderRequest(string Reply, decimal? QuotedPrice, SpecialOrderStatus? NewStatus);
public record UpdateSpecialOrderStatusRequest(SpecialOrderStatus Status);
public record UpdateRolesRequest(List<string> Roles);
public record ToggleApprovalRequest(bool IsApproved);
public record ReplyCommentRequest(string ReplyText);

