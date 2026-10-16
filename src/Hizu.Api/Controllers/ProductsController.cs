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
    private readonly IWebHostEnvironment _env;

    public ProductsController(IWebHostEnvironment env)
    {
        _env = env;
    }

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

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await Mediator.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
            return BadRequest("شناسه محصول همخوانی ندارد.");

        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProduct(Guid id)
    {
        await Mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpPatch("{id:guid}/stock")]
    public async Task<ActionResult<int?>> UpdateStock(Guid id, [FromBody] UpdateStockRequest request)
    {
        var result = await Mediator.Send(new UpdateProductStockCommand(id, request.QuantityChange));
        return Ok(result);
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpPost("upload-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
    public async Task<ActionResult<ProductImageUploadResult>> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("فایلی ارسال نشده است.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
        if (!allowedExtensions.Contains(ext))
            return BadRequest("فرمت فایل نامعتبر است. تنها فایل‌های تصویری مجاز هستند (WebP, JPG, PNG).");

        var uploadsFolder = Path.Combine(_env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot"), "uploads", "products");
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }

        var uniqueFileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var relativeUrl = $"/uploads/products/{uniqueFileName}";

        return Ok(new ProductImageUploadResult
        {
            ImageUrl = relativeUrl,
            FileName = file.FileName,
            FileSizeBytes = file.Length
        });
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<ActionResult<ProductCommentSummaryDto>> GetComments(Guid id)
    {
        var result = await Mediator.Send(new GetProductCommentsQuery(id));
        return Ok(result);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<ActionResult<ProductCommentDto>> CreateComment(Guid id, [FromBody] SubmitProductCommentRequest request)
    {
        var command = new CreateProductCommentCommand
        {
            ProductId = id,
            UserId = CurrentUserId,
            AuthorName = request.AuthorName,
            AuthorEmail = request.AuthorEmail ?? CurrentUserEmail,
            Content = request.Content,
            Rating = request.Rating
        };

        var result = await Mediator.Send(command);
        return Ok(result);
    }
}

public record UpdateStockRequest(int QuantityChange);

public record SubmitProductCommentRequest(
    string AuthorName,
    string? AuthorEmail,
    string Content,
    int Rating
);

public class ProductImageUploadResult
{
    public string ImageUrl { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}
