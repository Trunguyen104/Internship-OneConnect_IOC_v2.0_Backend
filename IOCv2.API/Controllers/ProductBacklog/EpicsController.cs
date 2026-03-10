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
public class EpicsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public EpicsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get all Epics for a project with pagination.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetEpicsResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEpics(
        [FromQuery] Guid projectId,
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
    [HttpGet("{id:guid}", Name = "GetEpicById")]
    [ProducesResponseType(typeof(ApiResponse<GetEpicByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEpicById(
        [FromRoute] Guid id,
        [FromQuery] Guid projectId,
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
    [ProducesResponseType(typeof(ApiResponse<CreateEpicResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEpic(
        [FromBody] CreateEpicCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetEpicById), new { id = result.Data?.Id, projectId = command.ProjectId, version = "1" });
    }

    /// <summary>
    /// Update an existing Epic.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateEpicResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEpic(
        [FromRoute] Guid id,
        [FromBody] UpdateEpicCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { EpicId = id }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft delete an Epic by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteEpicResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEpic(
        [FromRoute] Guid id,
        [FromBody] DeleteEpicCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { EpicId = id }, cancellationToken);
        return HandleResult(result);
    }
}
