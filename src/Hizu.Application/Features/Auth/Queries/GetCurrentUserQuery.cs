using Hizu.Application.Common.Exceptions;
using Hizu.Application.Features.Auth.DTOs;
using Hizu.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Hizu.Application.Features.Auth.Queries;

public record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;

public class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public GetCurrentUserQueryHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            throw new NotFoundException("User", request.UserId);

        var roles = await _userManager.GetRolesAsync(user);

        return new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Address = user.Address,
            Roles = roles.ToList(),
            CreatedAt = user.CreatedAt
        };
    }
}
