namespace Hizu.Application.Features.Products.DTOs;

public class ProductCommentDto
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public string? ProductName { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorEmail { get; set; }
    public string Content { get; set; } = string.Empty;
    public int Rating { get; set; }
    public bool IsApproved { get; set; }
    public string? AdminReply { get; set; }
    public DateTime? AdminRepliedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductCommentSummaryDto
{
    public double AverageRating { get; set; }
    public int TotalComments { get; set; }
    public List<ProductCommentDto> Comments { get; set; } = [];
}
