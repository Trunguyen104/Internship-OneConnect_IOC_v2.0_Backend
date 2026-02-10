using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue;
using IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue;
using IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus;
using IOCv2.Application.Features.StakeholderIssues.DTOs;
using IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssueById;
using IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues;
using IOCv2.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

[ApiController]
[Route("api/stakeholder-issues")]
//[Authorize]
public class StakeholderIssuesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IMessageService _messageService;

    public StakeholderIssuesController(IMediator mediator, IMessageService messageService)
    {
        _mediator = mediator;
        _messageService = messageService;
    }

    /// <summary>
    /// Lấy danh sách Issues (có phân trang)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<StakeholderIssueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIssues(
        [FromQuery] Guid? projectId, 
        [FromQuery] Guid? stakeholderId,
        [FromQuery] StakeholderIssueStatus? status, 
        [FromQuery] PaginationParams pagination)
    {
        var query = new GetStakeholderIssuesQuery
        {
            ProjectId = projectId,
            StakeholderId = stakeholderId,
            Status = status,
            Pagination = pagination
        };
        var result = await _mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Xem chi tiết Issue
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(StakeholderIssueDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIssueById(Guid id)
    {
        var result = await _mediator.Send(new GetStakeholderIssueByIdQuery(id));
        return HandleResult(result);
    }

    /// <summary>
    /// Tạo Issue mới
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateIssue([FromBody] CreateStakeholderIssueCommand command)
    {
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
        {
            return Ok(new
            {
                id = result.Data,
                message = _messageService.GetMessage("Issue.CreateSuccess")
            });
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Cập nhật trạng thái Issue
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIssueStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var command = new UpdateStakeholderIssueStatusCommand
        {
            Id = id,
            Status = request.Status
        };
        var result = await _mediator.Send(command);
        if (result.IsSuccess)
        {
            return Ok(new { message = result.Data });
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Xóa Issue
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIssue(Guid id)
    {
        var result = await _mediator.Send(new DeleteStakeholderIssueCommand(id));
        if (result.IsSuccess)
        {
            return Ok(new { message = result.Data });
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

    public class UpdateStatusRequest
    {
        public StakeholderIssueStatus Status { get; set; }
    }
}
