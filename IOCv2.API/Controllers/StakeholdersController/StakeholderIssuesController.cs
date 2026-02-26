using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue;
using IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue;
using IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus;
using IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssueById;
using IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.StakeholdersController
{
    /// <summary>
    /// Stakeholder Issue Management — manage issues related to stakeholders.
    /// </summary>
    [Tags("Stakeholder Issue Management")]
    //[Authorize]
    public class StakeholderIssuesController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public StakeholderIssuesController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get a paginated list of stakeholder issues with optional filters (projectId, stakeholderId, status) and search.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(Result<PagedResult<GetStakeholderIssuesResponse>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetIssues(
            [FromQuery] Guid? projectId,
            [FromQuery] Guid? stakeholderId,
            [FromQuery] string? status,
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
        /// Get a single stakeholder issue by ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<GetStakeholderIssueByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetIssueById(Guid id)
        {
            var result = await _mediator.Send(new GetStakeholderIssueByIdQuery(id));
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new stakeholder issue.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<CreateStakeholderIssueResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateIssue([FromBody] CreateStakeholderIssueCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Update the status of a stakeholder issue.
        /// </summary>
        [HttpPatch("{id:guid}/status")]
        [ProducesResponseType(typeof(Result<UpdateStakeholderIssueStatusResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateIssueStatus(Guid id, [FromBody] UpdateStatusRequest request)
        {
            var command = new UpdateStakeholderIssueStatusCommand
            {
                Id = id,
                Status = request.Status
            };

            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Delete (hard delete) a stakeholder issue by ID.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(Result<DeleteStakeholderIssueResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteIssue(Guid id)
        {
            var result = await _mediator.Send(new DeleteStakeholderIssueCommand { Id = id });
            return HandleResult(result);
        }

        public class UpdateStatusRequest
        {
            public string Status { get; set; } = string.Empty;
        }
    }
}

