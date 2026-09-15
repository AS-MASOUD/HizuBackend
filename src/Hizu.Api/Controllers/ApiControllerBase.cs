using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected Guid? CurrentUserId
    {
        get
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return Guid.TryParse(userIdString, out var userId) ? userId : null;
        }
    }

    protected string? CurrentUserEmail => User.FindFirstValue(ClaimTypes.Email);
}
