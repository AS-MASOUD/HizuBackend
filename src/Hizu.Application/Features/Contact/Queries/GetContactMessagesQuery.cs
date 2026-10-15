using Hizu.Application.Common.Interfaces;
using Hizu.Application.Common.Models;
using Hizu.Application.Features.Contact.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Contact.Queries;

public class GetContactMessagesQuery : PagedQuery, IRequest<PagedResult<ContactMessageDto>>
{
    public bool? IsRead { get; init; }
    public string? Search { get; init; }
}

public class GetContactMessagesQueryHandler : IRequestHandler<GetContactMessagesQuery, PagedResult<ContactMessageDto>>
{
    private readonly IApplicationDbContext _context;

    public GetContactMessagesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ContactMessageDto>> Handle(GetContactMessagesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ContactMessages
            .AsNoTracking()
            .AsQueryable();

        if (request.IsRead.HasValue)
        {
            query = query.Where(m => m.IsRead == request.IsRead.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(m =>
                m.Name.ToLower().Contains(search) ||
                m.Email.ToLower().Contains(search) ||
                (m.PhoneNumber != null && m.PhoneNumber.Contains(search)) ||
                (m.Subject != null && m.Subject.ToLower().Contains(search)) ||
                m.Message.ToLower().Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new ContactMessageDto
            {
                Id = m.Id,
                Name = m.Name,
                Email = m.Email,
                PhoneNumber = m.PhoneNumber,
                Subject = m.Subject,
                Message = m.Message,
                IsRead = m.IsRead,
                AdminNotes = m.AdminNotes,
                RepliedAt = m.RepliedAt,
                CreatedAt = m.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return PagedResult<ContactMessageDto>.Create(items, totalCount, request.Page, request.PageSize);
    }
}
