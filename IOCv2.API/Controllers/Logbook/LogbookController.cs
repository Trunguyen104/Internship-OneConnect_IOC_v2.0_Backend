using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Admin.Users.Commands.DeleteAdminUser;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook;
using IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook;
using IOCv2.Application.Features.Logbooks.Queries.GetLogbookById;
using IOCv2.Application.Features.Logbooks.Queries.GetLogbooks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

[ApiController]
[Route("api/logbooks")]
[Tags("Logbook")]
public class LogbookController : ControllerBase
{
    private readonly IMediator _mediator;

    public LogbookController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all logbooks
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetLogbooks([FromQuery] GetLogbooksQuery query)
    {
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get logbook by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetLogbookById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetLogbookByIdQuery { LogbookId = id });
        return HandleResult(result);
    }

    /// <summary>
    /// Create new logbook
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateLogbook([FromBody] CreateLogbookCommand command)
    {
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Update logbook
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateLogbook(
        [FromRoute] Guid id,
        [FromBody] UpdateLogbookCommand command)
    {
        var result = await _mediator.Send(command with { LogbookId = id });
        return HandleResult(result);
    }

    /// <summary>
    /// Delete logbook
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteLogbook([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new DeleteLogbookCommand { LogbookId = id });
        return HandleResult(result);
    }

    // Same helper như SprintController
    private IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
            return Ok(result.Data);

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(new { message = result.Error }),
            ResultErrorType.Unauthorized => Unauthorized(new { message = result.Error }),
            ResultErrorType.Conflict => Conflict(new { message = result.Error }),
            _ => BadRequest(new { message = result.Error })
        };
    }
}