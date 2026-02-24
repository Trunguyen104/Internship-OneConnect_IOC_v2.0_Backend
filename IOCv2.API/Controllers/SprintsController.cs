using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.Sprints.Commands.CompleteSprint;
using IOCv2.Application.Features.Sprints.Commands.CreateSprint;
using IOCv2.Application.Features.Sprints.Commands.DeleteSprint;
using IOCv2.Application.Features.Sprints.Commands.StartSprint;
using IOCv2.Application.Features.Sprints.Commands.UpdateSprint;
using IOCv2.Application.Features.Sprints.Queries.GetSprintById;
using IOCv2.Application.Features.Sprints.Queries.GetSprints;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

[ApiController]
[Route("api/projects/{projectId:guid}/sprints")]
// [Authorize]  // Disabled for testing
public class SprintsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public SprintsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Get all sprints for a project
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetSprints(
        [FromRoute] Guid projectId,
        [FromQuery] SprintStatus? status,
        [FromQuery] PaginationParams pagination)
    {
        var query = new GetSprintsQuery(projectId, status, pagination);
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }
    
    /// <summary>
    /// Get sprint by ID
    /// </summary>
    [HttpGet("{sprintId:guid}")]
    public async Task<IActionResult> GetSprintById(
        [FromRoute] Guid projectId,
        [FromRoute] Guid sprintId)
    {
        var query = new GetSprintByIdQuery(sprintId);
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }
    
    /// <summary>
    /// Create a new sprint
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSprint(
        [FromRoute] Guid projectId,
        [FromBody] CreateSprintRequest request)
    {
        var command = new CreateSprintCommand(
            projectId,
            request.Name,
            request.Goal);
        
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }
    
    /// <summary>
    /// Update an existing sprint
    /// </summary>
    [HttpPut("{sprintId:guid}")]
    public async Task<IActionResult> UpdateSprint(
        [FromRoute] Guid projectId,
        [FromRoute] Guid sprintId,
        [FromBody] UpdateSprintRequest request)
    {
        var command = new UpdateSprintCommand(
            sprintId,
            request.Name,
            request.Goal,
            request.StartDate,
            request.EndDate);
        
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }
    
    /// <summary>
    /// Delete a sprint (only Planned sprints can be deleted)
    /// </summary>
    [HttpDelete("{sprintId:guid}")]
    public async Task<IActionResult> DeleteSprint(
        [FromRoute] Guid projectId,
        [FromRoute] Guid sprintId)
    {
        var command = new DeleteSprintCommand(sprintId);
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }
    
    /// <summary>
    /// Start a sprint (change status from Planned to Active)
    /// </summary>
    [HttpPost("{sprintId:guid}/start")]
    public async Task<IActionResult> StartSprint(
        [FromRoute] Guid projectId,
        [FromRoute] Guid sprintId)
    {
        var command = new StartSprintCommand(sprintId);
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }
    
    /// <summary>
    /// Complete a sprint (change status from Active to Completed)
    /// </summary>
    [HttpPost("{sprintId:guid}/complete")]
    public async Task<IActionResult> CompleteSprint(
        [FromRoute] Guid projectId,
        [FromRoute] Guid sprintId,
        [FromBody] CompleteSprintCommand command)
    {
        // Ensure sprintId from route matches
        if (command.SprintId != sprintId)
            return BadRequest(new { errors = new[] { "SprintId mismatch" } });
        
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }
    
    // Helper methods to handle Result pattern - FIXED to match Epic format
    private IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }
        
        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(new { errors = result.Errors }),
            ResultErrorType.Unauthorized => Unauthorized(new { errors = result.Errors }),
            ResultErrorType.Conflict => Conflict(new { errors = result.Errors }),
            ResultErrorType.Validation => BadRequest(new { errors = result.Errors }),
            _ => BadRequest(new { errors = result.Errors })
        };
    }
    
    private IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok(new { message = "Operation completed successfully" });
        }
        
        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(new { errors = result.Errors }),
            ResultErrorType.Unauthorized => Unauthorized(new { errors = result.Errors }),
            ResultErrorType.Conflict => Conflict(new { errors = result.Errors }),
            ResultErrorType.Validation => BadRequest(new { errors = result.Errors }),
            _ => BadRequest(new { errors = result.Errors })
        };
    }
}
