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

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".step", ".stp", ".iges", ".igs", ".dxf", ".dwg", ".stl", ".obj", ".3dm",
        ".pdf", ".zip", ".rar", ".7z", ".png", ".jpg", ".jpeg", ".webp"
    };

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50 MB for CAD files / 3D models
    public async Task<ActionResult<SpecialOrderUploadItem>> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("فایلی ارسال نشده است.");

        var rawExtension = Path.GetExtension(file.FileName);
        if (string.IsNullOrWhiteSpace(rawExtension))
            return BadRequest("فایل فاقد پسوند معتبر است.");

        var extension = rawExtension.Trim().ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest("پسوند فایل مجاز نمی‌باشد. تنها فایل‌های طراحی مهندسی (STEP, DXF, STL, OBJ)، اسناد PDF، آرشیو فشرده و تصاویر مجاز هستند.");
        }

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "special-orders");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/special-orders/{uniqueFileName}";
        var sanitizedOriginalName = Path.GetFileName(file.FileName);

        return Ok(new SpecialOrderUploadItem
        {
            ImageUrl = relativeUrl,
            OriginalFileName = sanitizedOriginalName,
            FileSizeBytes = file.Length
        });
    }
}
