using Hizu.Domain.Common;

namespace Hizu.Domain.Entities;

public class Article : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? CoverImage { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? Tags { get; set; }
    public string Author { get; set; } = "تیم فنی هیزو";
    public string? AuthorRole { get; set; }
    public int ReadTimeMinutes { get; set; } = 5;
    public bool IsPublished { get; set; } = true;
    public DateTime? PublishedAt { get; set; }
    public int ViewsCount { get; set; } = 0;
}
