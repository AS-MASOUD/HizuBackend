using FluentValidation;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Contact.DTOs;
using Hizu.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Contact.Commands;

public record CreateContactMessageCommand : IRequest<ContactMessageDto>
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string? Subject { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class CreateContactMessageCommandValidator : AbstractValidator<CreateContactMessageCommand>
{
    public CreateContactMessageCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("نام و نام خانوادگی الزامی است.").MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().WithMessage("ایمیل الزامی است.").EmailAddress().WithMessage("فرمت ایمیل نامعتبر است.").MaximumLength(200);
        RuleFor(x => x.Message).NotEmpty().WithMessage("متن پیام الزامی است.").MaximumLength(4000);
        RuleFor(x => x.PhoneNumber).MaximumLength(30);
        RuleFor(x => x.Subject).MaximumLength(250);
    }
}

public class CreateContactMessageCommandHandler : IRequestHandler<CreateContactMessageCommand, ContactMessageDto>
{
    private readonly IApplicationDbContext _context;

    public CreateContactMessageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ContactMessageDto> Handle(CreateContactMessageCommand request, CancellationToken cancellationToken)
    {
        var message = new ContactMessage
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
            Subject = string.IsNullOrWhiteSpace(request.Subject) ? null : request.Subject.Trim(),
            Message = request.Message.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.ContactMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        return new ContactMessageDto
        {
            Id = message.Id,
            Name = message.Name,
            Email = message.Email,
            PhoneNumber = message.PhoneNumber,
            Subject = message.Subject,
            Message = message.Message,
            IsRead = message.IsRead,
            AdminNotes = message.AdminNotes,
            RepliedAt = message.RepliedAt,
            CreatedAt = message.CreatedAt
        };
    }
}

public record MarkContactMessageReadCommand(Guid Id, bool IsRead) : IRequest<bool>;

public class MarkContactMessageReadCommandHandler : IRequestHandler<MarkContactMessageReadCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public MarkContactMessageReadCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(MarkContactMessageReadCommand request, CancellationToken cancellationToken)
    {
        var message = await _context.ContactMessages
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (message == null)
            throw new NotFoundException("ContactMessage", request.Id);

        message.IsRead = request.IsRead;
        message.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record ReplyContactMessageCommand(Guid Id, string AdminNotes) : IRequest<bool>;

public class ReplyContactMessageCommandHandler : IRequestHandler<ReplyContactMessageCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ReplyContactMessageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ReplyContactMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _context.ContactMessages
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (message == null)
            throw new NotFoundException("ContactMessage", request.Id);

        message.AdminNotes = request.AdminNotes.Trim();
        message.RepliedAt = DateTime.UtcNow;
        message.IsRead = true;
        message.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

public record DeleteContactMessageCommand(Guid Id) : IRequest<bool>;

public class DeleteContactMessageCommandHandler : IRequestHandler<DeleteContactMessageCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteContactMessageCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(DeleteContactMessageCommand request, CancellationToken cancellationToken)
    {
        var message = await _context.ContactMessages
            .FirstOrDefaultAsync(m => m.Id == request.Id, cancellationToken);

        if (message == null)
            throw new NotFoundException("ContactMessage", request.Id);

        _context.ContactMessages.Remove(message);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
