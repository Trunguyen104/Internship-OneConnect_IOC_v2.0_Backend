using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.WorkItems.Commands.CreateWorkItem;
using IOCv2.Application.Features.WorkItems.Commands.DeleteWorkItem;
using IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToBacklog;
using IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToSprint;
using IOCv2.Application.Features.WorkItems.Commands.UpdateWorkItem;
using IOCv2.Application.Features.WorkItems.Queries.GetBacklog;
using IOCv2.Application.Features.WorkItems.Queries.GetWorkItemById;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.ProductBacklog;

/// <summary>
/// Manage Work Items (User Story, Task, Subtask) in the Product Backlog and Sprint Backlog.
/// </summary>
[Tags("WorkItems")]
[Authorize]
public class WorkItemsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public WorkItemsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get full backlog view — Sprint Backlogs + Product Backlog for a project.
    /// Supports filtering by Epic, Type, Priority, Status, Assignee, and search term.
    /// </summary>
    [HttpGet("backlog")]
    [ProducesResponseType(typeof(ApiResponse<GetBacklogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBacklog(
        [FromQuery] Guid projectId,
        [FromQuery] Guid? epicId,
        [FromQuery] string? searchTerm,
        [FromQuery] WorkItemType? type,
        [FromQuery] Priority? priority,
        [FromQuery] WorkItemStatus? status,
        [FromQuery] Guid? assigneeId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetBacklogQuery
        {
            ProjectId = projectId,
            EpicId = epicId,
            SearchTerm = searchTerm,
            Type = type,
            Priority = priority,
            Status = status,
            AssigneeId = assigneeId
        };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a work item by ID.
    /// </summary>
    [HttpGet("{workItemId:guid}", Name = "GetWorkItemById")]
    [ProducesResponseType(typeof(ApiResponse<GetWorkItemByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkItemById(
        [FromRoute] Guid workItemId,
        [FromQuery] Guid projectId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetWorkItemByIdQuery(projectId, workItemId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new work item. If SprintId is provided, the item is added to that Sprint's backlog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateWorkItemResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWorkItem(
        [FromBody] CreateWorkItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetWorkItemById), new { workItemId = result.Data?.WorkItemId, projectId = command.ProjectId, version = "1" });
    }

    /// <summary>
    /// Update a work item (PATCH-style — only fields provided are updated).
    /// </summary>
    [HttpPut("{workItemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateWorkItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWorkItem(
        [FromRoute] Guid workItemId,
        [FromBody] UpdateWorkItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { WorkItemId = workItemId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft-delete a work item.
    /// </summary>
    [HttpDelete("{workItemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteWorkItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkItem(
        [FromRoute] Guid workItemId,
        [FromBody] DeleteWorkItemCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { WorkItemId = workItemId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Move a work item to a Sprint (drag-and-drop from Product Backlog or another Sprint).
    /// AfterWorkItemId = null places the item at the top of the Sprint.
    /// </summary>
    [HttpPatch("{workItemId:guid}/sprint")]
    [ProducesResponseType(typeof(ApiResponse<MoveWorkItemToSprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveToSprint(
        [FromRoute] Guid workItemId,
        [FromBody] MoveWorkItemToSprintCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { WorkItemId = workItemId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Move a work item back to the Product Backlog.
    /// AfterWorkItemId = null places the item at the top of the backlog.
    /// </summary>
    [HttpPatch("{workItemId:guid}/backlog")]
    [ProducesResponseType(typeof(ApiResponse<MoveWorkItemToBacklogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveToBacklog(
        [FromRoute] Guid workItemId,
        [FromBody] MoveWorkItemToBacklogCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { WorkItemId = workItemId }, cancellationToken);
        return HandleResult(result);
    }
}
