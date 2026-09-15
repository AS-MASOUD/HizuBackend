using FluentValidation;
using Hizu.Application.Common.Exceptions;
using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Auth.DTOs;
using Hizu.Domain.Constants;
using Hizu.Domain.Entities;
using Hizu.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Hizu.Application.Features.Auth.Commands;

public record RegisterCommand : IRequest<AuthResponseDto>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public Address? Address { get; init; }
}

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6).WithMessage("کلمه عبور باید حداقل ۶ کاراکتر باشد.");
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("نام الزامی است.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("نام خانوادگی الزامی است.");
    }
}

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RegisterCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            throw new BadRequestException("کاربری با این ایمیل قبلاً ثبت‌نام کرده است.");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Address = request.Address,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new BadRequestException($"خطا در ثبت‌نام: {errors}");
        }

        await _userManager.AddToRoleAsync(user, AppRoles.Customer);

        var roles = new List<string> { AppRoles.Customer };
        var (token, expiration) = await _jwtTokenGenerator.GenerateTokenAsync(user, roles);

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                Roles = roles,
                CreatedAt = user.CreatedAt
            }
        };
    }
}

public record LoginCommand : IRequest<AuthResponseDto>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userManager = userManager;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            throw new UnauthorizedException("ایمیل یا کلمه عبور اشتباه است.");

        var isValidPassword = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isValidPassword)
            throw new UnauthorizedException("ایمیل یا کلمه عبور اشتباه است.");

        var roles = await _userManager.GetRolesAsync(user);
        var (token, expiration) = await _jwtTokenGenerator.GenerateTokenAsync(user, roles);

        return new AuthResponseDto
        {
            Token = token,
            Expiration = expiration,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Address = user.Address,
                Roles = roles.ToList(),
                CreatedAt = user.CreatedAt
            }
        };
    }
}

public record UpdateProfileCommand : IRequest<UserDto>
{
    public Guid UserId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public Address? Address { get; init; }
}

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserDto>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateProfileCommandHandler(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<UserDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
            throw new NotFoundException("User", request.UserId);

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        if (request.Address != null)
        {
            user.Address = request.Address;
        }
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException("بروزرسانی مشخصات با خطا مواجه شد.");

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
