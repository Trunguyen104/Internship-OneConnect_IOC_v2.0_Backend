using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Logbooks.Commands.CreateLogbook;
using IOCv2.Application.Features.Logbooks.Commands.DeleteLogbook;
using IOCv2.Application.Features.Logbooks.Commands.UpdateLogbook;
using IOCv2.Application.Features.Logbooks.Queries.GetLogbookById;
using IOCv2.Application.Features.Logbooks.Queries.GetLogbooks;
using IOCv2.Application.Features.Logbooks.Queries.GetMissingLogbookDates;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IOCv2.API.Controllers.Logbook;

/// <summary>
/// Logbook Management — manage internship logbook entries.
/// </summary>
[Tags("Logbooks")]
[Authorize]
public class LogbookController : ApiControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LogbookController> _logger;

    public LogbookController(IMediator mediator, ILogger<LogbookController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get logbooks grouped by week for an internship group.
    /// Use weekFilter (CSV) to select specific weeks, e.g. weekFilter=1,2.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetLogbooksByWeekResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetLogbooks([FromQuery] GetLogbooksQuery query)
    {
        _logger.LogInformation("Request to get logbooks for internship {InternshipId}", query.InternshipId);
        return HandleResult(await _mediator.Send(query));
    }

    /// <summary>
    /// Get a single logbook entry by ID.
    /// </summary>
    /// <param name="logbookId">ID of the logbook entry.</param>
    /// <returns code="200">Return details of the logbook.</returns>
    /// <returns code="404">Logbook entry not found.</returns>
    [HttpGet("{logbookId:guid}", Name = "GetLogbookById")]
    [ProducesResponseType(typeof(ApiResponse<GetLogbookByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogbookById([FromRoute] Guid logbookId)
    {
        _logger.LogInformation("Request to get logbook detail by ID: {LogbookId}", logbookId);
        return HandleResult(await _mediator.Send(new GetLogbookByIdQuery { LogbookId = logbookId }));
    }

    /// <summary>
    /// Create a new logbook entry.
    /// </summary>
    /// <param name="command">Logbook creation data.</param>
    /// <returns code="201">Returns the created logbook details.</returns>
    /// <returns code="400">Invalid logbook data.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateLogbookResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateLogbook([FromBody] CreateLogbookCommand command)
    {
        _logger.LogInformation("Request to create logbook for internship {InternshipId}", command.InternshipId);
        var result = await _mediator.Send(command);
        return HandleCreateResult(result, nameof(GetLogbookById), new { logbookId = result.Data?.LogbookId, version = "1" });
    }

    /// <summary>
    /// Update an existing logbook entry.
    /// </summary>
    [HttpPut("{logbookId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateLogbookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLogbook([FromRoute] Guid logbookId, [FromBody] UpdateLogbookCommand command)
    {
        _logger.LogInformation("Request to update logbook {LogbookId}", logbookId);
        command.LogbookId = logbookId;
        return HandleResult(await _mediator.Send(command));
    }


    /// <summary>
    /// Soft delete a logbook entry by ID.
    /// </summary>
    [HttpDelete("{logbookId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteLogbookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLogbook([FromRoute] Guid logbookId)
    {
        _logger.LogInformation("Request to delete logbook {LogbookId}", logbookId);
        return HandleResult(await _mediator.Send(new DeleteLogbookCommand { LogbookId = logbookId }));
    }

    /// <summary>
    /// Get the list of working days (Mon–Fri, excluding public holidays) on which
    /// a student has NOT submitted a logbook entry, from the start of their active
    /// internship phase up to today (UTC).
    /// </summary>
    /// <param name="studentId">
    /// Optional. The student to check. When omitted, the currently authenticated student is used.
    /// </param>
    /// <returns code="200">List of missing logbook dates and summary statistics.</returns>
    /// <returns code="404">No active internship found for the student.</returns>
    [HttpGet("missing-dates")]
    [ProducesResponseType(typeof(ApiResponse<GetMissingLogbookDatesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMissingLogbookDates([FromQuery] Guid? studentId = null)
    {
        _logger.LogInformation("Request to get missing logbook dates for StudentId={StudentId}", studentId);
        return HandleResult(await _mediator.Send(new GetMissingLogbookDatesQuery { StudentId = studentId }));
    }
}
