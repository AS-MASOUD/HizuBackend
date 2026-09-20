using Hizu.Application.Common.Exceptions;
using Hizu.Domain.Constants;
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

        var targetRoles = new HashSet<string>(request.Roles, StringComparer.OrdinalIgnoreCase);

        // Always preserve customer role so user can browse and order
        targetRoles.Add(AppRoles.Customer);

        // Prevent stripping SysManager from the primary root admin
        if (user.Email == "admin@hizu.ir" || user.UserName == "admin@hizu.ir")
        {
            targetRoles.Add(AppRoles.SysManager);
            targetRoles.Add(AppRoles.Admin);
        }

        foreach (var role in targetRoles)
        {
            if (!await _roleManager.RoleExistsAsync(role))
            {
                await _roleManager.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        var addResult = await _userManager.AddToRolesAsync(user, targetRoles);
        if (!addResult.Succeeded)
            throw new BadRequestException("خطا در افزودن نقش‌های جدید به کاربر.");

        return true;
    }
}
