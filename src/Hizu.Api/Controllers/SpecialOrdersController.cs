using Hizu.Application.Features.SpecialOrders.Commands;
using Hizu.Application.Features.SpecialOrders.DTOs;
using Hizu.Application.Features.SpecialOrders.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

public class SpecialOrdersController : ApiControllerBase
{
    private readonly IWebHostEnvironment _env;

    public SpecialOrdersController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpPost]
    public async Task<ActionResult<SpecialOrderDto>> CreateSpecialOrder([FromBody] CreateSpecialOrderCommand command)
    {
        var finalCommand = command with { UserId = CurrentUserId };
        var result = await Mediator.Send(finalCommand);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize]
    [HttpGet("my")]
    public async Task<ActionResult<List<SpecialOrderDto>>> GetMySpecialOrders()
    {
        if (!CurrentUserId.HasValue) return Unauthorized();
        var result = await Mediator.Send(new GetUserSpecialOrdersQuery(CurrentUserId.Value));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SpecialOrderDto>> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetSpecialOrderByIdQuery(id, CurrentUserId));
        return Ok(result);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB for CAD files / 3D models
    public async Task<ActionResult<SpecialOrderUploadItem>> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("فایلی ارسال نشده است.");

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "special-orders");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var extension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/special-orders/{uniqueFileName}";

        return Ok(new SpecialOrderUploadItem
        {
            ImageUrl = relativeUrl,
            OriginalFileName = file.FileName,
            FileSizeBytes = file.Length
        });
    }
}
