using Hizu.Application.Common.Interfaces;
using Hizu.Application.Features.Auth.Commands;
using Hizu.Application.Features.Auth.DTOs;
using Hizu.Application.Features.Auth.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

public class AuthController : ApiControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("send-otp")]
    public async Task<ActionResult<OtpSendResult>> SendOtp([FromBody] SendOtpCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("verify-otp-login")]
    public async Task<ActionResult<AuthResponseDto>> VerifyOtpLogin([FromBody] VerifyOtpLoginCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMe()
    {
        if (!CurrentUserId.HasValue)
            return Unauthorized();

        var result = await Mediator.Send(new GetCurrentUserQuery(CurrentUserId.Value));
        return Ok(result);
    }

    [Authorize]
    [HttpPut("profile")]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileCommand command)
    {
        if (!CurrentUserId.HasValue)
            return Unauthorized();

        var updateCommand = command with { UserId = CurrentUserId.Value };
        var result = await Mediator.Send(updateCommand);
        return Ok(result);
    }
}
