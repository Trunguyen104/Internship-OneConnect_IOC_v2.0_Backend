using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.Epics.Commands.CreateEpic;
using IOCv2.Application.Features.Epics.Commands.DeleteEpic;
using IOCv2.Application.Features.Epics.Commands.UpdateEpic;
using IOCv2.Application.Features.Epics.Queries.GetEpicById;
using IOCv2.Application.Features.Epics.Queries.GetEpics;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.ProductBacklog;

/// <summary>
/// Product Backlog — Epics management for a project.
/// </summary>
[Tags("Product Backlog - Epics")]
[Authorize]
[Route("api/projects/{projectId:guid}/epics")]
public class EpicsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public EpicsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all Epics for a project with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Result<PagedResult<GetEpicsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEpics(
        [FromRoute] Guid projectId,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEpicsQuery(projectId, pagination);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a single Epic by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(Result<GetEpicByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEpicById(
        [FromRoute] Guid projectId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEpicByIdQuery(projectId, id);
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new Epic for a project.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<CreateEpicResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateEpic(
        [FromRoute] Guid projectId,
        [FromBody] CreateEpicCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { ProjectId = projectId }, cancellationToken);
        return HandleCreatedResult(result);
    }

    /// <summary>
    /// Update an existing Epic.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(Result<UpdateEpicResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEpic(
        [FromRoute] Guid projectId,
        [FromRoute] Guid id,
        [FromBody] UpdateEpicCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { EpicId = id, ProjectId = projectId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft delete an Epic by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(Result<DeleteEpicResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEpic(
        [FromRoute] Guid projectId,
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var command = new DeleteEpicCommand(projectId, id);
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}
