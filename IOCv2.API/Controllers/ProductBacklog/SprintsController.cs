using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.Sprints.Commands.CompleteSprint;
using IOCv2.Application.Features.Sprints.Commands.CreateSprint;
using IOCv2.Application.Features.Sprints.Commands.DeleteSprint;
using IOCv2.Application.Features.Sprints.Commands.StartSprint;
using IOCv2.Application.Features.Sprints.Commands.UpdateSprint;
using IOCv2.Application.Features.Sprints.Queries.GetSprintById;
using IOCv2.Application.Features.Sprints.Queries.GetSprints;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.ProductBacklog;

/// <summary>
/// Product Backlog — Sprints management for a project.
/// </summary>
[Tags("Product Backlog - Sprints")]
[Authorize]
[Route("api/projects/{projectId:guid}/sprints")]
public class SprintsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public SprintsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all sprints for a project with optional status filter.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Result<GetSprintsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetSprints(
        [FromRoute] Guid projectId,
        [FromQuery] string? status,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSprintsQuery(projectId, status, pagination);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a single sprint by ID.
    /// </summary>
    [HttpGet("{sprintId:guid}")]
    [ProducesResponseType(typeof(Result<GetSprintByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSprintById(
        [FromRoute] Guid projectId,
        [FromRoute] Guid sprintId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetSprintByIdQuery(projectId, sprintId);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new sprint for a project.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<CreateSprintResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateSprint(
        [FromRoute] Guid projectId,
        [FromBody] CreateSprintCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { ProjectId = projectId }, cancellationToken);
        return HandleCreatedResult(result);
    }

    /// <summary>
    /// Update an existing sprint.
    /// </summary>
    [HttpPut("{sprintId:guid}")]
    [ProducesResponseType(typeof(Result<UpdateSprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSprint(
        [FromRoute] Guid projectId,
        [FromRoute] Guid sprintId,
        [FromBody] UpdateSprintCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { SprintId = sprintId, ProjectId = projectId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a sprint. Only sprints with Planned status can be deleted.
    /// </summary>
    [HttpDelete("{sprintId:guid}")]
    [ProducesResponseType(typeof(Result<DeleteSprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSprint(
        [FromRoute] Guid projectId,
        [FromRoute] Guid sprintId,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteSprintCommand(projectId, sprintId);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Start a sprint — transitions status from Planned to Active.
    /// </summary>
    [HttpPost("{sprintId:guid}/start")]
    [ProducesResponseType(typeof(Result<StartSprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartSprint(
        [FromRoute] Guid projectId,
        [FromRoute] Guid sprintId,
        [FromBody] StartSprintCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { SprintId = sprintId, ProjectId = projectId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Complete a sprint — transitions status from Active to Completed.
    /// </summary>
    [HttpPost("{sprintId:guid}/complete")]
    [ProducesResponseType(typeof(Result<CompleteSprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteSprint(
        [FromRoute] Guid projectId,
        [FromRoute] Guid sprintId,
        [FromBody] CompleteSprintCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { SprintId = sprintId, ProjectId = projectId }, cancellationToken);
        return HandleResult(result);
    }
}
