using FluentValidation;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Helpers;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Categories.DTOs;
using Hizu.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Categories.Commands;

public record CreateCategoryCommand : IRequest<CategoryDto>
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; } = true;
    public int DisplayOrder { get; init; } = 0;
    public int? ParentCategoryId { get; init; }
}

public class CreateCategoryCommandValidator : AbstractValidator<CreateCategoryCommand>
{
    public CreateCategoryCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(20);
    }
}

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _context;

    public CreateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug(request.Name)
            : SlugHelper.GenerateSlug(request.Slug);

        var existingSlug = await _context.Categories.AnyAsync(c => c.Slug == slug, cancellationToken);
        if (existingSlug)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        var category = new Category
        {
            Name = request.Name,
            Slug = slug,
            Code = request.Code.Trim().ToUpperInvariant(),
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            ParentCategoryId = request.ParentCategoryId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Code = category.Code,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder,
            ParentCategoryId = category.ParentCategoryId
        };
    }
}

public record UpdateCategoryCommand : IRequest<CategoryDto>
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
    public int DisplayOrder { get; init; }
    public int? ParentCategoryId { get; init; }
}

public class UpdateCategoryCommandHandler : IRequestHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category == null)
            throw new NotFoundException("Category", request.Id);

        category.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            category.Slug = SlugHelper.GenerateSlug(request.Slug);
        }
        category.Code = request.Code.Trim().ToUpperInvariant();
        category.Description = request.Description;
        category.ImageUrl = request.ImageUrl;
        category.IsActive = request.IsActive;
        category.DisplayOrder = request.DisplayOrder;
        category.ParentCategoryId = request.ParentCategoryId;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Slug = category.Slug,
            Code = category.Code,
            Description = category.Description,
            ImageUrl = category.ImageUrl,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder,
            ParentCategoryId = category.ParentCategoryId
        };
    }
}

public record DeleteCategoryCommand(int Id) : IRequest<bool>;

public class DeleteCategoryCommandHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteCategoryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (category == null)
            throw new NotFoundException("Category", request.Id);

        if (category.Products.Any())
            throw new BadRequestException("Cannot delete category that contains products.");

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
