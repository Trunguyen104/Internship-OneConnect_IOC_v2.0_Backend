using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.Epics.Commands.CreateEpic;
using IOCv2.Application.Features.Epics.Commands.DeleteEpic;
using IOCv2.Application.Features.Epics.Commands.UpdateEpic;
using IOCv2.Application.Features.Epics.Queries.GetEpicById;
using IOCv2.Application.Features.Epics.Queries.GetEpics;
using IOCv2.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

[ApiController]
[Route("api/projects/{projectId}/epics")]
public class EpicsController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public EpicsController(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    /// <summary>
    /// Get all Epics for a project with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<GetEpicsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEpics(
        Guid projectId,
        [FromQuery] PaginationParams pagination)
    {
        var query = new GetEpicsQuery(projectId, pagination);
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }
    
    /// <summary>
    /// Get Epic by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(GetEpicByIdResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEpicById(Guid projectId, Guid id)
    {
        var query = new GetEpicByIdQuery(id);
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }
    
    /// <summary>
    /// Create a new Epic
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateEpicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEpic(
        Guid projectId,
        [FromBody] CreateEpicRequest request)
    {
        var command = new CreateEpicCommand(projectId, request.Name, request.Description);
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }
    
    /// <summary>
    /// Update an existing Epic
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UpdateEpicResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateEpic(
        Guid projectId,
        Guid id,
        [FromBody] UpdateEpicRequest request)
    {
        var command = new UpdateEpicCommand(id, request.Name, request.Description);
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }
    
    /// <summary>
    /// Delete an Epic (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteEpic(Guid projectId, Guid id)
    {
        var command = new DeleteEpicCommand(id);
        var result = await _mediator.Send(command);
        return HandleResult(result);
    }
    
    // Helper method to handle Result pattern
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

