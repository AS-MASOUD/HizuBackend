using FluentValidation;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Products.DTOs;
using Hizu.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Products.Commands;

public record CreateProductCommentCommand : IRequest<ProductCommentDto>
{
    public Guid ProductId { get; init; }
    public string AuthorName { get; init; } = string.Empty;
    public string? AuthorEmail { get; init; }
    public string Content { get; init; } = string.Empty;
    public int Rating { get; init; } = 5;
    public Guid? UserId { get; init; }
}

public class CreateProductCommentCommandValidator : AbstractValidator<CreateProductCommentCommand>
{
    public CreateProductCommentCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.AuthorName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
    }
}

public class CreateProductCommentCommandHandler : IRequestHandler<CreateProductCommentCommand, ProductCommentDto>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductCommentDto> Handle(CreateProductCommentCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync([request.ProductId], cancellationToken);
        if (product == null)
            throw new NotFoundException("Product", request.ProductId);

        var comment = new ProductComment
        {
            ProductId = request.ProductId,
            UserId = request.UserId,
            AuthorName = request.AuthorName.Trim(),
            AuthorEmail = request.AuthorEmail?.Trim(),
            Content = request.Content.Trim(),
            Rating = Math.Clamp(request.Rating, 1, 5),
            IsApproved = false, // Must be approved by admin
            CreatedAt = DateTime.UtcNow
        };

        _context.ProductComments.Add(comment);
        await _context.SaveChangesAsync(cancellationToken);

        return new ProductCommentDto
        {
            Id = comment.Id,
            ProductId = comment.ProductId,
            ProductName = product.Name,
            AuthorName = comment.AuthorName,
            AuthorEmail = comment.AuthorEmail,
            Content = comment.Content,
            Rating = comment.Rating,
            IsApproved = comment.IsApproved,
            AdminReply = comment.AdminReply,
            AdminRepliedAt = comment.AdminRepliedAt,
            CreatedAt = comment.CreatedAt
        };
    }
}

public record ApproveProductCommentCommand(Guid CommentId, bool IsApproved) : IRequest<bool>;

public class ApproveProductCommentCommandHandler : IRequestHandler<ApproveProductCommentCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ApproveProductCommentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ApproveProductCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _context.ProductComments.FindAsync([request.CommentId], cancellationToken);
        if (comment == null)
            throw new NotFoundException("ProductComment", request.CommentId);

        comment.IsApproved = request.IsApproved;
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record ReplyProductCommentCommand(Guid CommentId, string ReplyText, Guid? AdminUserId = null) : IRequest<ProductCommentDto>;

public class ReplyProductCommentCommandHandler : IRequestHandler<ReplyProductCommentCommand, ProductCommentDto>
{
    private readonly IApplicationDbContext _context;

    public ReplyProductCommentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductCommentDto> Handle(ReplyProductCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _context.ProductComments
            .Include(c => c.Product)
            .FirstOrDefaultAsync(c => c.Id == request.CommentId, cancellationToken);

        if (comment == null)
            throw new NotFoundException("ProductComment", request.CommentId);

        comment.AdminReply = request.ReplyText.Trim();
        comment.AdminRepliedAt = DateTime.UtcNow;
        comment.AdminUserId = request.AdminUserId;
        comment.IsApproved = true; // Auto-approve when replied by admin
        comment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductCommentDto
        {
            Id = comment.Id,
            ProductId = comment.ProductId,
            ProductName = comment.Product?.Name,
            AuthorName = comment.AuthorName,
            AuthorEmail = comment.AuthorEmail,
            Content = comment.Content,
            Rating = comment.Rating,
            IsApproved = comment.IsApproved,
            AdminReply = comment.AdminReply,
            AdminRepliedAt = comment.AdminRepliedAt,
            CreatedAt = comment.CreatedAt
        };
    }
}

public record DeleteProductCommentCommand(Guid CommentId) : IRequest<bool>;

public class DeleteProductCommentCommandHandler : IRequestHandler<DeleteProductCommentCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteProductCommentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteProductCommentCommand request, CancellationToken cancellationToken)
    {
        var comment = await _context.ProductComments.FindAsync([request.CommentId], cancellationToken);
        if (comment == null)
            throw new NotFoundException("ProductComment", request.CommentId);

        _context.ProductComments.Remove(comment);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
