using Hizu.Application.Common.Exceptions;
using Hizu.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Hizu.Application.Features.Admin.Commands;

public record UpdateUserRolesCommand(Guid UserId, List<string> Roles) : IRequest<bool>;

public class UpdateUserRolesCommandHandler : IRequestHandler<UpdateUserRolesCommand, bool>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;

    public UpdateUserRolesCommandHandler(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<bool> Handle(UpdateUserRolesCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            throw new NotFoundException("User", request.UserId);

        var currentRoles = await _userManager.GetRolesAsync(user);
        var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
        if (!removeResult.Succeeded)
            throw new BadRequestException("خطا در حذف نقش‌های قبلی کاربر.");

        foreach (var role in request.Roles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var addResult = await _userManager.AddToRolesAsync(user, request.Roles);
        if (!addResult.Succeeded)
            throw new BadRequestException("خطا در افزودن نقش‌های جدید به کاربر.");

        return true;
    }
}
