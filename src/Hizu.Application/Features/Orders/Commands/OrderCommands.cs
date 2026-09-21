using FluentValidation;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Orders.DTOs;
using Hizu.Domain.Entities;
using Hizu.Domain.Enums;
using Hizu.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Orders.Commands;

public record CreateOrderCommand : IRequest<OrderDto>
{
    public Guid UserId { get; init; }
    public Address ShippingAddress { get; init; } = new();
}

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.ShippingAddress.Street).NotEmpty().WithMessage("آدرس پستی الزامی است.");
        RuleFor(x => x.ShippingAddress.City).NotEmpty().WithMessage("نام شهر الزامی است.");
        RuleFor(x => x.ShippingAddress.State).NotEmpty().WithMessage("نام استان الزامی است.");
    }
}

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;

    public CreateOrderCommandHandler(
        IApplicationDbContext context,
        ISmsService smsService,
        IEmailService emailService)
    {
        _context = context;
        _smsService = smsService;
        _emailService = emailService;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var cart = await _context.Carts
            .Include(c => c.Items)
                .ThenInclude(i => i.Product)
            .Include(c => c.Items)
                .ThenInclude(i => i.Course)
            .FirstOrDefaultAsync(c => c.UserId == request.UserId, cancellationToken);

        if (cart == null || !cart.Items.Any())
            throw new BadRequestException("سبد خرید شما خالی است.");

        var user = await _context.Users.FindAsync([request.UserId], cancellationToken);
        if (user == null)
            throw new NotFoundException("User", request.UserId);

        // Transactional execution with atomic inventory deduction
        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            decimal subtotal = 0;
            var orderItems = new List<OrderItem>();

            foreach (var item in cart.Items)
            {
                if (item.CourseId.HasValue)
                {
                    var course = await _context.Courses.FindAsync([item.CourseId.Value], cancellationToken);
                    if (course == null || !course.IsPublished)
                        throw new BadRequestException($"دوره آموزشی '{item.Course?.Title ?? "نامشخص"}' در حال حاضر فعال یا منتشر شده نیست.");

                    var itemTotal = course.Price * 1;
                    subtotal += itemTotal;

                    course.StudentsCount += 1;
                    course.UpdatedAt = DateTime.UtcNow;

                    orderItems.Add(new OrderItem
                    {
                        CourseId = course.Id,
                        ProductId = null,
                        ProductName = course.Title,
                        Sku = $"CRS-{course.Slug.ToUpper()}",
                        Quantity = 1,
                        UnitPrice = course.Price,
                        TotalPrice = itemTotal,
                        CreatedAt = DateTime.UtcNow
                    });

                    // Add enrollment for the user
                    var existingEnrollment = await _context.CourseEnrollments
                        .FirstOrDefaultAsync(e => e.UserId == request.UserId && e.CourseId == course.Id, cancellationToken);
                    if (existingEnrollment == null)
                    {
                        _context.CourseEnrollments.Add(new CourseEnrollment
                        {
                            UserId = request.UserId,
                            CourseId = course.Id,
                            EnrolledAt = DateTime.UtcNow,
                            IsCompleted = false
                        });
                    }
                }
                else if (item.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync([item.ProductId.Value], cancellationToken);
                    if (product == null || !product.IsActive)
                        throw new BadRequestException($"محصول '{item.Product?.Name ?? "نامشخص"}' در حال حاضر موجود یا فعال نیست.");

                    if (product.StockQuantity < item.Quantity)
                        throw new BadRequestException($"موجودی محصول '{product.Name}' برای تعداد درخواستی کافی نیست. موجودی انبار: {product.StockQuantity}");

                    var itemTotal = product.Price * item.Quantity;
                    subtotal += itemTotal;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        CourseId = null,
                        ProductName = product.Name,
                        Sku = product.Sku,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price, // Preserves price at purchase time
                        TotalPrice = itemTotal,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            // Digital courses have free shipping. If order has physical products and subtotal <= 1500000, shipping is 75000.
            bool hasPhysicalProducts = orderItems.Any(i => i.ProductId.HasValue);
            decimal shippingCost = (!hasPhysicalProducts || subtotal > 1500000) ? 0 : 75000;
            decimal total = subtotal + shippingCost;

            var orderNumber = $"HIZU-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            var order = new Order
            {
                UserId = request.UserId,
                OrderNumber = orderNumber,
                Status = OrderStatus.Pending, // Starts in Pending; stock deducted when committed from admin
                IsStockDeducted = false,
                Subtotal = subtotal,
                ShippingCost = shippingCost,
                Total = total,
                ShippingAddress = request.ShippingAddress,
                Items = orderItems,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);

            // Clear the user's cart
            _context.CartItems.RemoveRange(cart.Items);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            // Build OrderDto for return and rich notifications
            var customerFullName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(customerFullName)) customerFullName = "کاربر گرامی";

            var orderDto = new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                CustomerName = customerFullName,
                CustomerEmail = user.Email ?? string.Empty,
                CustomerPhone = user.PhoneNumber ?? string.Empty,
                Status = order.Status,
                IsStockDeducted = order.IsStockDeducted,
                Subtotal = order.Subtotal,
                ShippingCost = order.ShippingCost,
                Total = order.Total,
                ShippingAddress = order.ShippingAddress,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    CourseId = i.CourseId,
                    ProductName = i.ProductName,
                    Sku = i.Sku,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };

            var itemsSummary = string.Join("، ", order.Items.Select(i => $"{i.ProductName} ({i.Quantity}x)"));
            var destinationCity = !string.IsNullOrWhiteSpace(order.ShippingAddress.City) ? order.ShippingAddress.City : "نامشخص";

            // 1. Customer notifications
            try
            {
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    await _smsService.SendCustomerOrderStatusSmsAsync(user.PhoneNumber, customerFullName, order.OrderNumber, "در انتظار بررسی و تایید", cancellationToken);
                }
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    await _emailService.SendCustomerOrderStatusEmailAsync(
                        user.Email,
                        customerFullName,
                        order.OrderNumber,
                        "در انتظار بررسی و تایید",
                        "سفارش شما با موفقیت در سیستم استودیو هیزو ثبت گردید و در انتظار بررسی و تایید قرار دارد.",
                        orderDto,
                        cancellationToken);
                }
            }
            catch
            {
                // Silently log or continue to ensure order creation flow never fails on notification timeouts
            }

            // 2. Admin alert notifications (SMS to 09101854076 & full details email to as.masoud.1868@gmail.com)
            try
            {
                await _smsService.SendAdminOrderAlertSmsAsync(
                    order.OrderNumber,
                    customerFullName,
                    user.PhoneNumber ?? "ثبت‌نشده",
                    order.Total,
                    itemsSummary,
                    destinationCity,
                    cancellationToken);

                await _emailService.SendAdminOrderFullDetailsEmailAsync(
                    orderDto,
                    user.PhoneNumber ?? string.Empty,
                    cancellationToken);
            }
            catch
            {
                // Notification failure does not void transaction
            }

            return orderDto;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

public record UpdateOrderStatusCommand(Guid OrderId, OrderStatus NewStatus) : IRequest<bool>;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;

    public UpdateOrderStatusCommandHandler(
        IApplicationDbContext context,
        ISmsService smsService,
        IEmailService emailService)
    {
        _context = context;
        _smsService = smsService;
        _emailService = emailService;
    }

    public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken);

        if (order == null)
            throw new NotFoundException("Order", request.OrderId);

        // Stock inventory management on admin commit / uncommit
        bool isCommitStatus = request.NewStatus == OrderStatus.Processing ||
                              request.NewStatus == OrderStatus.Shipped ||
                              request.NewStatus == OrderStatus.Delivered;

        bool isUncommitStatus = request.NewStatus == OrderStatus.Pending ||
                                request.NewStatus == OrderStatus.Cancelled;

        if (isCommitStatus && !order.IsStockDeducted)
        {
            foreach (var item in order.Items)
            {
                if (item.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync([item.ProductId.Value], cancellationToken);
                    if (product != null)
                    {
                        product.StockQuantity = Math.Max(0, product.StockQuantity - item.Quantity);
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
            order.IsStockDeducted = true;
        }
        else if (isUncommitStatus && order.IsStockDeducted)
        {
            foreach (var item in order.Items)
            {
                if (item.ProductId.HasValue)
                {
                    var product = await _context.Products.FindAsync([item.ProductId.Value], cancellationToken);
                    if (product != null)
                    {
                        product.StockQuantity += item.Quantity;
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }
            }
            order.IsStockDeducted = false;
        }

        order.Status = request.NewStatus;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Notify customer via SMS and Email
        var user = await _context.Users.FindAsync([order.UserId], cancellationToken);
        if (user != null)
        {
            var customerName = $"{user.FirstName} {user.LastName}".Trim();
            if (string.IsNullOrWhiteSpace(customerName)) customerName = "کاربر گرامی";

            var (statusTitle, statusDescription) = request.NewStatus switch
            {
                OrderStatus.Pending => (
                    "در انتظار بررسی و پرداخت",
                    "سفارش شما در سیستم ثبت شده و در انتظار بررسی و تایید قرار دارد."
                ),
                OrderStatus.Processing => (
                    "در حال آماده‌سازی و ساخت",
                    "سفارش شما تایید گردید و در حال حاضر در کارگاه استودیو هیزو در مرحله آماده‌سازی و ساخت قطعات می‌باشد."
                ),
                OrderStatus.Shipped => (
                    "تحویل به اداره پست جهت ارسال",
                    "سفارش شما با موفقیت بسته‌بندی شده و جهت ارسال به آدرس شما، تحویل مأمور اداره پست گردید."
                ),
                OrderStatus.Delivered => (
                    "تحویل داده شده به مشتری",
                    "سفارش شما تحویل داده شد. از حسن انتخاب و اعتماد شما به استودیو طراحی و ساخت هیزو سپاسگزاریم."
                ),
                OrderStatus.Cancelled => (
                    "لغو شده",
                    "سفارش شما لغو گردید. در صورت نیاز به راهنمایی یا پیگیری مالی، با بخش پشتیبانی هیزو تماس حاصل فرمایید."
                ),
                _ => (request.NewStatus.ToString(), string.Empty)
            };

            var orderDto = new OrderDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                UserId = order.UserId,
                CustomerName = customerName,
                CustomerEmail = user.Email ?? string.Empty,
                CustomerPhone = user.PhoneNumber ?? string.Empty,
                Status = order.Status,
                IsStockDeducted = order.IsStockDeducted,
                Subtotal = order.Subtotal,
                ShippingCost = order.ShippingCost,
                Total = order.Total,
                ShippingAddress = order.ShippingAddress,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    CourseId = i.CourseId,
                    ProductName = i.ProductName,
                    Sku = i.Sku,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    TotalPrice = i.TotalPrice
                }).ToList()
            };

            // 1. Customer SMS
            if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
            {
                try
                {
                    await _smsService.SendCustomerOrderStatusSmsAsync(
                        user.PhoneNumber,
                        customerName,
                        order.OrderNumber,
                        statusTitle,
                        cancellationToken);
                }
                catch
                {
                    // Ignore SMS transport error to not break API call
                }
            }

            // 2. Customer Email
            if (!string.IsNullOrWhiteSpace(user.Email))
            {
                try
                {
                    await _emailService.SendCustomerOrderStatusEmailAsync(
                        user.Email,
                        customerName,
                        order.OrderNumber,
                        statusTitle,
                        statusDescription,
                        orderDto,
                        cancellationToken);
                }
                catch
                {
                    // Ignore Email transport error to not break API call
                }
            }
        }

        return true;
    }
}
