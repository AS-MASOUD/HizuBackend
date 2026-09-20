using FluentValidation;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.SpecialOrders.DTOs;
using Hizu.Domain.Entities;
using Hizu.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.SpecialOrders.Commands;

public record CreateSpecialOrderCommand : IRequest<SpecialOrderDto>
{
    public Guid? UserId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Category { get; init; }
    public string Description { get; init; } = string.Empty;
    public string? MaterialPreference { get; init; }
    public decimal? EstimatedBudget { get; init; }
    public List<SpecialOrderUploadItem> UploadedFiles { get; init; } = [];
}

public class SpecialOrderUploadItem
{
    public string ImageUrl { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public long FileSizeBytes { get; set; }
}

public class CreateSpecialOrderCommandValidator : AbstractValidator<CreateSpecialOrderCommand>
{
    public CreateSpecialOrderCommandValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().WithMessage("نام و نام خانوادگی الزامی است.");
        RuleFor(x => x.CustomerPhone).NotEmpty().WithMessage("شماره تماس الزامی است.");
        RuleFor(x => x.Title).NotEmpty().WithMessage("عنوان پروژه یا قطعه الزامی است.");
        RuleFor(x => x.Description).NotEmpty().WithMessage("شرح مشخصات و نیاز فنی الزامی است.");
    }
}

public class CreateSpecialOrderCommandHandler : IRequestHandler<CreateSpecialOrderCommand, SpecialOrderDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;

    public CreateSpecialOrderCommandHandler(
        IApplicationDbContext context,
        ISmsService smsService,
        IEmailService emailService)
    {
        _context = context;
        _smsService = smsService;
        _emailService = emailService;
    }

    public async Task<SpecialOrderDto> Handle(CreateSpecialOrderCommand request, CancellationToken cancellationToken)
    {
        var orderNumber = $"FAB-{DateTime.UtcNow:yyyyMMdd}-{new Random().Next(1000, 9999)}";

        var specialOrder = new SpecialOrder
        {
            OrderNumber = orderNumber,
            UserId = request.UserId,
            CustomerName = request.CustomerName.Trim(),
            CustomerPhone = request.CustomerPhone.Trim(),
            CustomerEmail = request.CustomerEmail?.Trim(),
            Title = request.Title.Trim(),
            Category = request.Category,
            Description = request.Description.Trim(),
            MaterialPreference = request.MaterialPreference,
            EstimatedBudget = request.EstimatedBudget,
            Status = SpecialOrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var file in request.UploadedFiles)
        {
            specialOrder.Images.Add(new SpecialOrderImage
            {
                ImageUrl = file.ImageUrl,
                OriginalFileName = file.OriginalFileName,
                FileSizeBytes = file.FileSizeBytes,
                CreatedAt = DateTime.UtcNow
            });
        }

        _context.SpecialOrders.Add(specialOrder);
        await _context.SaveChangesAsync(cancellationToken);

        // 1. Customer notification
        if (!string.IsNullOrWhiteSpace(specialOrder.CustomerPhone))
        {
            await _smsService.SendOrderNotificationAsync(
                specialOrder.CustomerPhone,
                specialOrder.OrderNumber,
                "درخواست ساخت اختصاصی شما با موفقیت ثبت شد و در دست بررسی کارشناسان قرار گرفت.",
                cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(specialOrder.CustomerEmail))
        {
            await _emailService.SendOrderEmailAsync(
                specialOrder.CustomerEmail,
                specialOrder.OrderNumber,
                $"درخواست ساخت قطعه اختصاصی با عنوان «{specialOrder.Title}» با موفقیت در هایزو ثبت گردید.",
                cancellationToken);
        }

        // 2. Admin alert notification
        var adminAlert = $"درخواست ساخت قطعه جدید #{specialOrder.OrderNumber} با عنوان «{specialOrder.Title}» از طرف {specialOrder.CustomerName} ثبت شد.";
        await _smsService.SendAdminNotificationAsync(adminAlert, cancellationToken);
        await _emailService.SendAdminOrderAlertEmailAsync(
            specialOrder.OrderNumber,
            specialOrder.EstimatedBudget ?? 0,
            specialOrder.CustomerName,
            cancellationToken);

        return new SpecialOrderDto
        {
            Id = specialOrder.Id,
            OrderNumber = specialOrder.OrderNumber,
            UserId = specialOrder.UserId,
            CustomerName = specialOrder.CustomerName,
            CustomerPhone = specialOrder.CustomerPhone,
            CustomerEmail = specialOrder.CustomerEmail,
            Title = specialOrder.Title,
            Category = specialOrder.Category,
            Description = specialOrder.Description,
            MaterialPreference = specialOrder.MaterialPreference,
            EstimatedBudget = specialOrder.EstimatedBudget,
            Status = specialOrder.Status,
            CreatedAt = specialOrder.CreatedAt,
            Images = specialOrder.Images.Select(i => new SpecialOrderImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                OriginalFileName = i.OriginalFileName,
                FileSizeBytes = i.FileSizeBytes
            }).ToList()
        };
    }
}

public record ReplySpecialOrderCommand(Guid Id, string Reply, decimal? QuotedPrice, SpecialOrderStatus? NewStatus) : IRequest<bool>;

public class ReplySpecialOrderCommandHandler : IRequestHandler<ReplySpecialOrderCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ISmsService _smsService;
    private readonly IEmailService _emailService;

    public ReplySpecialOrderCommandHandler(
        IApplicationDbContext context,
        ISmsService smsService,
        IEmailService emailService)
    {
        _context = context;
        _smsService = smsService;
        _emailService = emailService;
    }

    public async Task<bool> Handle(ReplySpecialOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.SpecialOrders.FindAsync([request.Id], cancellationToken);
        if (order == null)
            throw new NotFoundException("SpecialOrder", request.Id);

        order.AdminReply = request.Reply;
        order.AdminRepliedAt = DateTime.UtcNow;

        if (request.QuotedPrice.HasValue)
        {
            order.QuotedPrice = request.QuotedPrice.Value;
        }

        if (request.NewStatus.HasValue)
        {
            order.Status = request.NewStatus.Value;
        }

        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        // Notify customer of admin response
        var messageDetail = request.QuotedPrice.HasValue
            ? $"برآورد هزینه: {request.QuotedPrice.Value:N0} تومان. پاسخ: {request.Reply}"
            : $"پاسخ هایزو: {request.Reply}";

        if (!string.IsNullOrWhiteSpace(order.CustomerPhone))
        {
            await _smsService.SendOrderNotificationAsync(
                order.CustomerPhone,
                order.OrderNumber,
                $"به درخواست ساخت اختصاصی شما پاسخ داده شد: {messageDetail}",
                cancellationToken);
        }
        if (!string.IsNullOrWhiteSpace(order.CustomerEmail))
        {
            await _emailService.SendOrderEmailAsync(
                order.CustomerEmail,
                order.OrderNumber,
                $"پاسخ کارشناس هایزو به درخواست شما: <br/><strong>{messageDetail}</strong>",
                cancellationToken);
        }

        return true;
    }
}

public record UpdateSpecialOrderStatusCommand(Guid Id, SpecialOrderStatus Status) : IRequest<bool>;

public class UpdateSpecialOrderStatusCommandHandler : IRequestHandler<UpdateSpecialOrderStatusCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly ISmsService _smsService;

    public UpdateSpecialOrderStatusCommandHandler(
        IApplicationDbContext context,
        ISmsService smsService)
    {
        _context = context;
        _smsService = smsService;
    }

    public async Task<bool> Handle(UpdateSpecialOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _context.SpecialOrders.FindAsync([request.Id], cancellationToken);
        if (order == null)
            throw new NotFoundException("SpecialOrder", request.Id);

        order.Status = request.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(order.CustomerPhone))
        {
            await _smsService.SendOrderNotificationAsync(
                order.CustomerPhone,
                order.OrderNumber,
                $"وضعیت درخواست شما به «{request.Status}» تغییر یافت.",
                cancellationToken);
        }

        return true;
    }
}
