using FluentValidation;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Cart.DTOs;
using Hizu.Application.Features.Cart.Queries;
using Hizu.Domain.Entities;
using CartEntity = Hizu.Domain.Entities.Cart;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Cart.Commands;

public record AddToCartCommand(Guid UserId, Guid? ProductId = null, int Quantity = 1, Guid? CourseId = null) : IRequest<CartDto>;

public class AddToCartCommandValidator : AbstractValidator<AddToCartCommand>
{
    public AddToCartCommandValidator()
    {
        RuleFor(x => x)
            .Must(x => x.ProductId.HasValue || x.CourseId.HasValue)
            .WithMessage("شناسه محصول یا شناسه دوره آموزشی باید مشخص باشد.");

        RuleFor(x => x.Quantity).GreaterThan(0);
    }
}

public class AddToCartCommandHandler : IRequestHandler<AddToCartCommand, CartDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public AddToCartCommandHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<CartDto> Handle(AddToCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null)
        {
            cart = new CartEntity
            {
                UserId = request.UserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Carts.Add(cart);
        }

        if (request.CourseId.HasValue)
        {
            var course = await _context.Courses.FindAsync([request.CourseId.Value], cancellationToken);
            if (course == null || !course.IsPublished)
                throw new NotFoundException("Course", request.CourseId.Value);

            if (course.IsReservationOnly || !course.Price.HasValue || course.Price.Value <= 0)
                throw new BadRequestException("این دوره در مرحله پیش‌ثبت‌نام و رزرو است و نیازی به سبد خرید ندارد. لطفاً از دکمه رزرو مستقیم دوره استفاده فرمایید.");

            var existingItem = cart.Items.FirstOrDefault(i => i.CourseId == request.CourseId.Value);
            if (existingItem == null)
            {
                var cartItem = new CartItem
                {
                    Cart = cart,
                    CartId = cart.Id,
                    Course = course,
                    CourseId = course.Id,
                    ProductId = null,
                    Quantity = 1,
                    UnitPrice = course.Price.Value,
                    CreatedAt = DateTime.UtcNow
                };
                cart.Items.Add(cartItem);
                _context.CartItems.Add(cartItem);
            }
        }
        else if (request.ProductId.HasValue)
        {
            var product = await _context.Products.FindAsync([request.ProductId.Value], cancellationToken);
            if (product == null || !product.IsActive)
                throw new NotFoundException("Product", request.ProductId.Value);

            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId.Value);
            var totalRequested = (existingItem?.Quantity ?? 0) + request.Quantity;

            if (totalRequested > product.StockQuantity)
                throw new BadRequestException($"امکان افزودن {request.Quantity} عدد وجود ندارد. موجودی انبار: {product.StockQuantity} عدد");

            if (existingItem != null)
            {
                existingItem.Quantity = totalRequested;
                existingItem.UnitPrice = product.Price;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var cartItem = new CartItem
                {
                    Cart = cart,
                    CartId = cart.Id,
                    Product = product,
                    ProductId = request.ProductId.Value,
                    CourseId = null,
                    Quantity = request.Quantity,
                    UnitPrice = product.Price,
                    CreatedAt = DateTime.UtcNow
                };
                cart.Items.Add(cartItem);
                _context.CartItems.Add(cartItem);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCartQuery(request.UserId), cancellationToken);
    }
}

public record UpdateCartItemCommand(Guid UserId, Guid CartItemId, int Quantity) : IRequest<CartDto>;

public class UpdateCartItemCommandHandler : IRequestHandler<UpdateCartItemCommand, CartDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public UpdateCartItemCommandHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<CartDto> Handle(UpdateCartItemCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null)
            throw new NotFoundException("Cart for user", request.UserId);

        var item = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId);
        if (item == null)
            throw new NotFoundException("CartItem", request.CartItemId);

        if (request.Quantity <= 0)
        {
            _context.CartItems.Remove(item);
        }
        else if (item.CourseId.HasValue)
        {
            item.Quantity = 1;
            item.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            if (item.Product != null && request.Quantity > item.Product.StockQuantity)
                throw new BadRequestException($"تعداد درخواستی ({request.Quantity}) بیشتر از موجودی انبار ({item.Product.StockQuantity}) است.");

            item.Quantity = request.Quantity;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCartQuery(request.UserId), cancellationToken);
    }
}

public record RemoveFromCartCommand(Guid UserId, Guid CartItemId) : IRequest<CartDto>;

public class RemoveFromCartCommandHandler : IRequestHandler<RemoveFromCartCommand, CartDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediator _mediator;

    public RemoveFromCartCommandHandler(IApplicationDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public async Task<CartDto> Handle(RemoveFromCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart != null)
        {
            var item = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId);
            if (item != null)
            {
                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync(cancellationToken);
            }
        }

        return await _mediator.Send(new GetCartQuery(request.UserId), cancellationToken);
    }
}

public record ClearCartCommand(Guid UserId) : IRequest<bool>;

public class ClearCartCommandHandler : IRequestHandler<ClearCartCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ClearCartCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ClearCartCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart != null)
        {
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync(cancellationToken);
        }

        return true;
    }
}
