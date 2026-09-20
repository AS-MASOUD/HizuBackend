using Hizu.Application.Features.Cart.Commands;
using Hizu.Application.Features.Cart.DTOs;
using Hizu.Application.Features.Cart.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

[Authorize]
public class CartController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        var result = await Mediator.Send(new GetCartQuery(CurrentUserId.Value));
        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddCartItemRequest request)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        var result = await Mediator.Send(new AddToCartCommand(CurrentUserId.Value, request.ProductId, request.Quantity, request.CourseId));
        return Ok(result);
    }

    [HttpPut("items/{id:guid}")]
    public async Task<ActionResult<CartDto>> UpdateItem(Guid id, [FromBody] UpdateCartItemRequest request)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        var result = await Mediator.Send(new UpdateCartItemCommand(CurrentUserId.Value, id, request.Quantity));
        return Ok(result);
    }

    [HttpDelete("items/{id:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid id)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        var result = await Mediator.Send(new RemoveFromCartCommand(CurrentUserId.Value, id));
        return Ok(result);
    }

    [HttpDelete]
    public async Task<ActionResult> ClearCart()
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        await Mediator.Send(new ClearCartCommand(CurrentUserId.Value));
        return NoContent();
    }
}

public record AddCartItemRequest(Guid? ProductId = null, int Quantity = 1, Guid? CourseId = null);
public record UpdateCartItemRequest(int Quantity);
