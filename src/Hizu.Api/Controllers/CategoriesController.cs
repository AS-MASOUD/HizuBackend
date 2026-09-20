using Hizu.Application.Features.Categories.Commands;
using Hizu.Application.Features.Categories.DTOs;
using Hizu.Application.Features.Categories.Queries.GetCategories;
using Hizu.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

public class CategoriesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories([FromQuery] bool onlyActive = true)
    {
        var result = await Mediator.Send(new GetCategoriesQuery(onlyActive));
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetCategories), new { id = result.Id }, result);
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryCommand command)
    {
        if (id != command.Id)
            return BadRequest("شناسه دسته‌بندی همخوانی ندارد.");

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        await Mediator.Send(new DeleteCategoryCommand(id));
        return NoContent();
    }
}
