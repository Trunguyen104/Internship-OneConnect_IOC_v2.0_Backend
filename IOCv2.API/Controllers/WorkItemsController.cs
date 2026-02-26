using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.WorkItems.Commands.CreateWorkItem;
using IOCv2.Application.Features.WorkItems.Commands.DeleteWorkItem;
using IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToBacklog;
using IOCv2.Application.Features.WorkItems.Commands.MoveWorkItemToSprint;
using IOCv2.Application.Features.WorkItems.Commands.UpdateWorkItem;
using IOCv2.Application.Features.WorkItems.Queries.GetBacklog;
using IOCv2.Application.Features.WorkItems.Queries.GetWorkItemById;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

/// <summary>
/// Quản lý Work Items (User Story, Task, Subtask) trong Product Backlog và Sprint Backlog.
/// </summary>
[Tags("WorkItems")]
[ApiController]
[Route("api/projects/{projectId:guid}/work-items")]
// [Authorize]
public class WorkItemsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public WorkItemsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get full backlog view — Sprint Backlogs + Product Backlog for a project.
    /// Supports filtering by Epic, Type, Priority, Status, Assignee, and search term.
    /// </summary>
    [HttpGet("backlog")]
    [ProducesResponseType(typeof(Result<GetBacklogResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBacklog(
        [FromRoute] Guid projectId,
        [FromQuery] Guid? epicId,
        [FromQuery] string? searchTerm,
        [FromQuery] string? type,
        [FromQuery] string? priority,
        [FromQuery] string? status,
        [FromQuery] Guid? assigneeId)
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
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a work item by ID.
    /// </summary>
    [HttpGet("{workItemId:guid}")]
    [ProducesResponseType(typeof(Result<GetWorkItemByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkItemById(
        [FromRoute] Guid projectId,
        [FromRoute] Guid workItemId)
    {
        var result = await _mediator.Send(new GetWorkItemByIdQuery(workItemId));
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new work item. If SprintId is provided, the item is added to that Sprint's backlog.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Result<CreateWorkItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWorkItem(
        [FromRoute] Guid projectId,
        [FromBody] CreateWorkItemCommand command)
    {
        var result = await _mediator.Send(command with { ProjectId = projectId });
        return HandleResult(result);
    }

    /// <summary>
    /// Update a work item (PATCH-style — only fields provided are updated).
    /// </summary>
    [HttpPut("{workItemId:guid}")]
    [ProducesResponseType(typeof(Result<UpdateWorkItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWorkItem(
        [FromRoute] Guid projectId,
        [FromRoute] Guid workItemId,
        [FromBody] UpdateWorkItemCommand command)
    {
        var result = await _mediator.Send(command with { WorkItemId = workItemId });
        return HandleResult(result);
    }

    /// <summary>
    /// Soft-delete a work item.
    /// </summary>
    [HttpDelete("{workItemId:guid}")]
    [ProducesResponseType(typeof(Result<DeleteWorkItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWorkItem(
        [FromRoute] Guid projectId,
        [FromRoute] Guid workItemId)
    {
        var result = await _mediator.Send(new DeleteWorkItemCommand { WorkItemId = workItemId });
        return HandleResult(result);
    }

    /// <summary>
    /// Move a work item to a Sprint (drag-and-drop from Product Backlog or another Sprint).
    /// AfterWorkItemId = null places the item at the top of the Sprint.
    /// </summary>
    [HttpPost("{workItemId:guid}/move-to-sprint")]
    [ProducesResponseType(typeof(Result<MoveWorkItemToSprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveToSprint(
        [FromRoute] Guid projectId,
        [FromRoute] Guid workItemId,
        [FromBody] MoveWorkItemToSprintCommand command)
    {
        var result = await _mediator.Send(command with { WorkItemId = workItemId });
        return HandleResult(result);
    }

    /// <summary>
    /// Move a work item back to the Product Backlog.
    /// AfterWorkItemId = null places the item at the top of the backlog.
    /// </summary>
    [HttpPost("{workItemId:guid}/move-to-backlog")]
    [ProducesResponseType(typeof(Result<MoveWorkItemToBacklogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveToBacklog(
        [FromRoute] Guid projectId,
        [FromRoute] Guid workItemId,
        [FromBody] MoveWorkItemToBacklogCommand command)
    {
        var result = await _mediator.Send(command with { WorkItemId = workItemId });
        return HandleResult(result);
    }
}
