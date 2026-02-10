using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder;
using IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder;
using IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder;
using IOCv2.Application.Features.Stakeholders.DTOs;
using IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById;
using IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

[ApiController]
[Route("api/stakeholders")]
//[Authorize]
public class StakeholdersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMessageService _messageService;

    public StakeholdersController(IMediator mediator, IMessageService messageService)
    {
        _mediator = mediator;
        _messageService = messageService;
    }

    /// <summary>
    /// Get list of stakeholders for a project
    /// </summary>
    [HttpGet("project/{projectId:guid}")]
    [ProducesResponseType(typeof(PagedResult<StakeholderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStakeholders(Guid projectId, [FromQuery] PaginationParams pagination)
    {
        var query = new GetStakeholdersQuery(projectId, pagination);
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Get stakeholder details by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StakeholderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStakeholderById(Guid id)
    {
        var query = new GetStakeholderByIdQuery(id);
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new stakeholder
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateStakeholder([FromBody] CreateStakeholderCommand command)
    {
        var result = await _mediator.Send(command);
        
        if (result.IsSuccess)
        {
            return Ok(new 
            { 
                id = result.Data,
                message = _messageService.GetMessage("Stakeholder.CreateSuccess") 
            });
        }
        
        return HandleResult(result);
    }

    /// <summary>
    /// Update stakeholder information
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStakeholder(Guid id, [FromBody] UpdateStakeholderCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest(new { message = _messageService.GetMessage("Stakeholder.IdMismatch") });
        }

        var result = await _mediator.Send(command);
        
        if (result.IsSuccess)
        {
            return Ok(new { message = _messageService.GetMessage("Stakeholder.UpdateSuccess") });
        }
        
        return HandleResult(result);
    }

    /// <summary>
    /// Delete a stakeholder
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteStakeholder(Guid id)
    {
        var command = new DeleteStakeholderCommand(id);
        var result = await _mediator.Send(command);
        
        if (result.IsSuccess)
        {
            return Ok(new { message = _messageService.GetMessage("Stakeholder.DeleteSuccess") });
        }
        
        return HandleResult(result);
    }

    private IActionResult HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Data);
        }

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(new { message = result.Errors.FirstOrDefault() }),
            ResultErrorType.Unauthorized => Unauthorized(new { message = result.Errors.FirstOrDefault() }),
            ResultErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Errors.FirstOrDefault() }),
            ResultErrorType.Conflict => Conflict(new { message = result.Errors.FirstOrDefault() }),
            ResultErrorType.Validation => BadRequest(new { errors = result.Errors }),
            _ => BadRequest(new { message = result.Errors.FirstOrDefault() })
        };
    }

    private IActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return result.ErrorType switch
        {
            ResultErrorType.NotFound => NotFound(new { message = result.Errors.FirstOrDefault() }),
            ResultErrorType.Unauthorized => Unauthorized(new { message = result.Errors.FirstOrDefault() }),
            ResultErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = result.Errors.FirstOrDefault() }),
            ResultErrorType.Conflict => Conflict(new { message = result.Errors.FirstOrDefault() }),
            ResultErrorType.Validation => BadRequest(new { errors = result.Errors }),
            _ => BadRequest(new { message = result.Errors.FirstOrDefault() })
        };
    }
}

