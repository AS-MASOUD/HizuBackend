using Hizu.Application.Common.Models;
using Hizu.Application.Features.Products.Commands;
using Hizu.Application.Features.Products.DTOs;
using Hizu.Application.Features.Products.Queries;
using Hizu.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

public class ProductsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductListDto>>> GetProducts([FromQuery] GetProductsQuery query)
    {
        var result = await Mediator.Send(query);
        return Ok(result);
    }

    [HttpGet("featured")]
    public async Task<ActionResult<List<ProductListDto>>> GetFeatured([FromQuery] int count = 4)
    {
        var result = await Mediator.Send(new GetFeaturedProductsQuery(count));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetProductByIdQuery(id));
        return Ok(result);
    }

    [HttpGet("slug/{slug}")]
    public async Task<ActionResult<ProductDto>> GetBySlug(string slug)
    {
        var result = await Mediator.Send(new GetProductBySlugQuery(slug));
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
            return BadRequest("شناسه محصول همخوانی ندارد.");

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        await Mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPatch("{id:guid}/stock")]
    public async Task<ActionResult<int>> UpdateStock(Guid id, [FromBody] UpdateStockRequest request)
    {
        var result = await Mediator.Send(new UpdateProductStockCommand(id, request.QuantityChange));
        return Ok(result);
    }
}

public record UpdateStockRequest(int QuantityChange);
