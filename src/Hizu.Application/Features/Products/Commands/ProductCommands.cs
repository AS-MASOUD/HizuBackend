using FluentValidation;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Helpers;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Products.DTOs;
using Hizu.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Products.Commands;

public record CreateProductCommand : IRequest<ProductDto>
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public int StockQuantity { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; } = true;
    public bool IsFeatured { get; init; } = false;

    public string? Dimensions { get; init; }
    public string? Weight { get; init; }
    public string? Material { get; init; }
    public string? Color { get; init; }
    public string? Technique { get; init; }
    public string? Tolerance { get; init; }

    public int CategoryId { get; init; }
    public List<string> ImageUrls { get; init; } = [];
}

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Price).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CategoryId).GreaterThan(0);
    }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var category = await _context.Categories.FindAsync([request.CategoryId], cancellationToken);
        if (category == null)
            throw new NotFoundException("Category", request.CategoryId);

        var slug = string.IsNullOrWhiteSpace(request.Slug)
            ? SlugHelper.GenerateSlug(request.Name)
            : SlugHelper.GenerateSlug(request.Slug);

        var existingSlug = await _context.Products.AnyAsync(p => p.Slug == slug, cancellationToken);
        if (existingSlug)
        {
            slug = $"{slug}-{DateTime.UtcNow.Ticks % 10000}";
        }

        var product = new Product
        {
            Name = request.Name,
            Slug = slug,
            Description = request.Description,
            ShortDescription = request.ShortDescription,
            Sku = request.Sku.Trim().ToUpperInvariant(),
            Price = request.Price,
            CompareAtPrice = request.CompareAtPrice,
            StockQuantity = request.StockQuantity,
            ImageUrl = request.ImageUrl ?? request.ImageUrls.FirstOrDefault(),
            IsActive = request.IsActive,
            IsFeatured = request.IsFeatured,
            Dimensions = request.Dimensions,
            Weight = request.Weight,
            Material = request.Material,
            Color = request.Color,
            Technique = request.Technique,
            Tolerance = request.Tolerance,
            CategoryId = request.CategoryId,
            CreatedAt = DateTime.UtcNow
        };

        int displayOrder = 1;
        foreach (var url in request.ImageUrls)
        {
            product.Images.Add(new ProductImage
            {
                ImageUrl = url,
                DisplayOrder = displayOrder++
            });
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            Sku = product.Sku,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            Dimensions = product.Dimensions,
            Weight = product.Weight,
            Material = product.Material,
            Color = product.Color,
            Technique = product.Technique,
            Tolerance = product.Tolerance,
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            CategorySlug = category.Slug
        };
    }
}

public record UpdateProductCommand : IRequest<ProductDto>
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? ShortDescription { get; init; }
    public string Sku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public decimal? CompareAtPrice { get; init; }
    public int StockQuantity { get; init; }
    public string? ImageUrl { get; init; }
    public bool IsActive { get; init; }
    public bool IsFeatured { get; init; }

    public string? Dimensions { get; init; }
    public string? Weight { get; init; }
    public string? Material { get; init; }
    public string? Color { get; init; }
    public string? Technique { get; init; }
    public string? Tolerance { get; init; }

    public int CategoryId { get; init; }
    public List<string>? ImageUrls { get; init; }
}

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .Include(p => p.Category)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

        if (product == null)
            throw new NotFoundException("Product", request.Id);

        var category = await _context.Categories.FindAsync([request.CategoryId], cancellationToken);
        if (category == null)
            throw new NotFoundException("Category", request.CategoryId);

        product.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Slug))
        {
            product.Slug = SlugHelper.GenerateSlug(request.Slug);
        }
        product.Description = request.Description;
        product.ShortDescription = request.ShortDescription;
        product.Sku = request.Sku.Trim().ToUpperInvariant();
        product.Price = request.Price;
        product.CompareAtPrice = request.CompareAtPrice;
        product.StockQuantity = request.StockQuantity;
        product.ImageUrl = request.ImageUrl;
        product.IsActive = request.IsActive;
        product.IsFeatured = request.IsFeatured;
        product.Dimensions = request.Dimensions;
        product.Weight = request.Weight;
        product.Material = request.Material;
        product.Color = request.Color;
        product.Technique = request.Technique;
        product.Tolerance = request.Tolerance;
        product.CategoryId = request.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        if (request.ImageUrls != null)
        {
            product.Images.Clear();
            int order = 1;
            foreach (var url in request.ImageUrls)
            {
                product.Images.Add(new ProductImage
                {
                    ImageUrl = url,
                    DisplayOrder = order++
                });
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Description = product.Description,
            ShortDescription = product.ShortDescription,
            Sku = product.Sku,
            Price = product.Price,
            CompareAtPrice = product.CompareAtPrice,
            StockQuantity = product.StockQuantity,
            ImageUrl = product.ImageUrl,
            IsActive = product.IsActive,
            IsFeatured = product.IsFeatured,
            Dimensions = product.Dimensions,
            Weight = product.Weight,
            Material = product.Material,
            Color = product.Color,
            Technique = product.Technique,
            Tolerance = product.Tolerance,
            CategoryId = product.CategoryId,
            CategoryName = category.Name,
            CategorySlug = category.Slug
        };
    }
}

public record DeleteProductCommand(Guid Id) : IRequest<bool>;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync([request.Id], cancellationToken);
        if (product == null)
            throw new NotFoundException("Product", request.Id);

        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record UpdateProductStockCommand(Guid Id, int QuantityChange) : IRequest<int>;

public class UpdateProductStockCommandHandler : IRequestHandler<UpdateProductStockCommand, int>
{
    private readonly IApplicationDbContext _context;

    public UpdateProductStockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(UpdateProductStockCommand request, CancellationToken cancellationToken)
    {
        var product = await _context.Products.FindAsync([request.Id], cancellationToken);
        if (product == null)
            throw new NotFoundException("Product", request.Id);

        var newStock = product.StockQuantity + request.QuantityChange;
        if (newStock < 0)
            throw new BadRequestException($"Insufficient stock for product '{product.Name}'. Available: {product.StockQuantity}");

        product.StockQuantity = newStock;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return product.StockQuantity;
    }
}
