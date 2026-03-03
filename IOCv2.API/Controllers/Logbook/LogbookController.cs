using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook;
using IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook;
using IOCv2.Application.Features.Logbooks.Queries.GetLogbookById;
using IOCv2.Application.Features.Logbooks.Queries.GetLogbooks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Logbook;

/// <summary>
/// Logbook Management — manage internship logbook entries.
/// </summary>
[Tags("Logbooks")]
[Authorize]
[Route("api/logbooks")]
public class LogbookController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public LogbookController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get paginated list of logbook entries with optional filters.
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
    /// Get a single logbook entry by ID.
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
    /// Create a new logbook entry.
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
    /// Update an existing logbook entry.
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
    /// Soft delete a logbook entry by ID.
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
}