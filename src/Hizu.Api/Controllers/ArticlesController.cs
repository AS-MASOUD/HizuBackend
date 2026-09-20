using Hizu.Application.Common.Interfaces;
using Hizu.Domain.Constants;
using Hizu.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Api.Controllers;

public class ArticlesController : ApiControllerBase
{
    private readonly IApplicationDbContext _context;

    public ArticlesController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ArticleDto>>> GetArticles(
        [FromQuery] string? category = null,
        [FromQuery] string? search = null,
        [FromQuery] bool? onlyPublished = null)
    {
        var query = _context.Articles.AsQueryable();

        if (onlyPublished == true || (onlyPublished == null && !User.IsInRole(AppRoles.Admin) && !User.IsInRole(AppRoles.SysManager)))
        {
            query = query.Where(a => a.IsPublished);
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "همه")
        {
            query = query.Where(a => a.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(a =>
                a.Title.ToLower().Contains(s) ||
                a.Summary.ToLower().Contains(s) ||
                (a.Tags != null && a.Tags.ToLower().Contains(s)));
        }

        var articles = await query
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var dtos = articles.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ArticleDto>> GetBySlug(string slug)
    {
        var article = await _context.Articles
            .FirstOrDefaultAsync(a => a.Slug == slug);

        if (article == null && Guid.TryParse(slug, out var id))
        {
            article = await _context.Articles.FindAsync(id);
        }

        if (article == null)
        {
            return NotFound("مقاله مورد نظر یافت نشد.");
        }

        // Increment view count
        article.ViewsCount++;
        await _context.SaveChangesAsync();

        return Ok(MapToDto(article));
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpPost]
    public async Task<ActionResult<ArticleDto>> CreateArticle([FromBody] CreateArticleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("عنوان و متن مقاله الزامی است.");
        }

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? request.Title.Trim().ToLower().Replace(" ", "-")
            : request.Slug.Trim().ToLower().Replace(" ", "-");

        var existing = await _context.Articles.AnyAsync(a => a.Slug == slug);
        if (existing)
        {
            slug += $"-{DateTime.UtcNow.Ticks % 10000}";
        }

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Title = request.Title.Trim(),
            Slug = slug,
            Summary = request.Summary?.Trim() ?? string.Empty,
            Content = request.Content,
            CoverImage = request.CoverImage,
            Category = request.Category?.Trim() ?? "پرینت ۳بعدی",
            Tags = request.Tags != null ? string.Join(",", request.Tags) : null,
            Author = request.Author?.Trim() ?? "تیم فنی هیزو",
            AuthorRole = request.AuthorRole?.Trim() ?? "استودیو هیزو",
            ReadTimeMinutes = request.ReadTimeMinutes ?? 5,
            IsPublished = request.IsPublished ?? true,
            PublishedAt = (request.IsPublished ?? true) ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow,
            ViewsCount = 0
        };

        _context.Articles.Add(article);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBySlug), new { slug = article.Slug }, MapToDto(article));
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ArticleDto>> UpdateArticle(Guid id, [FromBody] UpdateArticleRequest request)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null)
        {
            return NotFound("مقاله مورد نظر یافت نشد.");
        }

        article.Title = request.Title.Trim();
        article.Slug = request.Slug.Trim();
        article.Summary = request.Summary?.Trim() ?? string.Empty;
        article.Content = request.Content;
        article.CoverImage = request.CoverImage;
        article.Category = request.Category?.Trim() ?? "پرینت ۳بعدی";
        article.Tags = request.Tags != null ? string.Join(",", request.Tags) : null;
        article.Author = request.Author?.Trim() ?? "تیم فنی هیزو";
        article.AuthorRole = request.AuthorRole?.Trim() ?? "استودیو هیزو";
        article.ReadTimeMinutes = request.ReadTimeMinutes ?? 5;
        article.UpdatedAt = DateTime.UtcNow;

        if (!article.IsPublished && request.IsPublished)
        {
            article.PublishedAt = DateTime.UtcNow;
        }
        article.IsPublished = request.IsPublished;

        await _context.SaveChangesAsync();
        return Ok(MapToDto(article));
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteArticle(Guid id)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null)
        {
            return NotFound("مقاله مورد نظر یافت نشد.");
        }

        _context.Articles.Remove(article);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [Authorize(Roles = AppRoles.AdminOrSysManager)]
    [HttpPatch("{id:guid}/publish")]
    public async Task<ActionResult> TogglePublish(Guid id, [FromBody] TogglePublishRequest request)
    {
        var article = await _context.Articles.FindAsync(id);
        if (article == null)
        {
            return NotFound("مقاله مورد نظر یافت نشد.");
        }

        article.IsPublished = request.IsPublished;
        article.UpdatedAt = DateTime.UtcNow;
        if (article.IsPublished && article.PublishedAt == null)
        {
            article.PublishedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static ArticleDto MapToDto(Article a)
    {
        return new ArticleDto
        {
            Id = a.Id.ToString(),
            Title = a.Title,
            Slug = a.Slug,
            Summary = a.Summary,
            Content = a.Content,
            CoverImage = a.CoverImage ?? string.Empty,
            Category = a.Category,
            Tags = !string.IsNullOrWhiteSpace(a.Tags)
                ? a.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
                : new List<string>(),
            Author = a.Author,
            AuthorRole = a.AuthorRole ?? "استودیو هیزو",
            ReadTimeMinutes = a.ReadTimeMinutes,
            IsPublished = a.IsPublished,
            PublishedAt = a.PublishedAt?.ToString("o"),
            CreatedAt = a.CreatedAt.ToString("o"),
            UpdatedAt = a.UpdatedAt?.ToString("o"),
            ViewsCount = a.ViewsCount
        };
    }
}

public class ArticleDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string CoverImage { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string Author { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public int ReadTimeMinutes { get; set; }
    public bool IsPublished { get; set; }
    public string? PublishedAt { get; set; }
    public string CreatedAt { get; set; } = string.Empty;
    public string? UpdatedAt { get; set; }
    public int ViewsCount { get; set; }
}

public class CreateArticleRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Slug { get; set; }
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string? Category { get; set; }
    public List<string>? Tags { get; set; }
    public string? Author { get; set; }
    public string? AuthorRole { get; set; }
    public int? ReadTimeMinutes { get; set; }
    public bool? IsPublished { get; set; }
}

public class UpdateArticleRequest
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string? Category { get; set; }
    public List<string>? Tags { get; set; }
    public string? Author { get; set; }
    public string? AuthorRole { get; set; }
    public int? ReadTimeMinutes { get; set; }
    public bool IsPublished { get; set; }
}

public class TogglePublishRequest
{
    public bool IsPublished { get; set; }
}
