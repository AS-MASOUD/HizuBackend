using Hizu.Application.Features.Contact.Commands;
using Hizu.Application.Features.Contact.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Hizu.Api.Controllers;

public class ContactController : ApiControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ContactMessageDto>> SendMessage([FromBody] CreateContactMessageCommand command)
    {
        var result = await Mediator.Send(command);
        return Ok(result);
    }
}
