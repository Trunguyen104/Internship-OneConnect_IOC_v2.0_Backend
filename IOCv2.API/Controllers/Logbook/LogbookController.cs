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

/// <summary>
/// Logbook Management — manage internship logbook entries.
/// </summary>
[Tags("Logbooks")]
[Authorize]
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
    [HttpGet("/api/projects/{projectId:guid}/logbooks")]
    [ProducesResponseType(typeof(Result<GetLogbooksResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLogbooks(
        [FromRoute] Guid projectId,
        [FromQuery] GetLogbooksQuery query,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(query with { ProjectId = projectId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get logbook by ID
    /// </summary>
    [HttpGet("/api/projects/{projectId:guid}/logbooks/{logbookId:guid}")]
    [ProducesResponseType(typeof(Result<GetLogbookByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogbookById(
        [FromRoute] Guid projectId,
        [FromRoute] Guid logbookId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetLogbookByIdQuery { LogbookId = logbookId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create new logbook
    /// </summary>
    [HttpPost("/api/projects/{projectId:guid}/logbooks")]
    [ProducesResponseType(typeof(Result<CreateLogbookResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateLogbook(
        [FromRoute] Guid projectId,
        [FromBody] CreateLogbookCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { ProjectId = projectId }, cancellationToken);
        return HandleCreatedResult(result);
    }

    /// <summary>
    /// Update logbook
    /// </summary>
    [HttpPut("/api/projects/{projectId:guid}/logbooks/{logbookId:guid}")]
    [ProducesResponseType(typeof(Result<UpdateLogbookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLogbook(
        [FromRoute] Guid projectId,
        [FromRoute] Guid logbookId,
        [FromBody] UpdateLogbookCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { LogbookId = logbookId}, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete logbook
    /// </summary>
    [HttpDelete("/api/projects/{projectId:guid}/logbooks/{logbookId:guid}")]
    [ProducesResponseType(typeof(Result<DeleteLogbookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLogbook(
        [FromRoute] Guid projectId,
        [FromRoute] Guid logbookId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteLogbookCommand { ProjectId = projectId, LogbookId = logbookId }, cancellationToken);
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