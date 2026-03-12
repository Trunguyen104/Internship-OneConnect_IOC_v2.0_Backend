using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue;
using IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue;
using IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus;
using IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssueById;
using IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Stakeholders;

/// <summary>
/// Stakeholder Issue Management — manage issues related to stakeholders.
/// </summary>
[Tags("Stakeholder Issue Management")]
[Authorize]
public class StakeholderIssuesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public StakeholderIssuesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get a paginated list of stakeholder issues with optional filters (internshipId, stakeholderId, status) and search.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetStakeholderIssuesResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIssues(
        [FromQuery] Guid? internshipId,
        [FromQuery] Guid? stakeholderId,
        [FromQuery] StakeholderIssueStatus? status,
        [FromQuery] PaginationParams pagination,
        CancellationToken cancellationToken = default)
    {
        var query = new GetStakeholderIssuesQuery
        {
            InternshipId = internshipId,
            StakeholderId = stakeholderId,
            Status = status,
            Pagination = pagination
        };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get a single stakeholder issue by ID.
    /// </summary>
    [HttpGet("{id:guid}", Name = "GetStakeholderIssueById")]
    [ProducesResponseType(typeof(ApiResponse<GetStakeholderIssueByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIssueById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetStakeholderIssueByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new stakeholder issue.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateStakeholderIssueResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateIssue(
        [FromBody] CreateStakeholderIssueCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetIssueById), new { id = result.Data?.Id, version = "1" });
    }

    /// <summary>
    /// Update the status of a stakeholder issue.
    /// </summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<UpdateStakeholderIssueStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateIssueStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateStakeholderIssueStatusCommand
        {
            Id = id,
            Status = request.Status
        };
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Delete (hard delete) a stakeholder issue by ID.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<DeleteStakeholderIssueResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIssue(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DeleteStakeholderIssueCommand { Id = id }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Request body for updating stakeholder issue status.
    /// </summary>
    public class UpdateStatusRequest
    {
        public StakeholderIssueStatus Status { get; set; }
    }
}
