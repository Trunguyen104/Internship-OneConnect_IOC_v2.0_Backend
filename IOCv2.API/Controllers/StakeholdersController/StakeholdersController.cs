using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder;
using IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder;
using IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder;
using IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById;
using IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.StakeholdersController
{
    /// <summary>
    /// Stakeholder Management — manage stakeholders for a project.
    /// </summary>
    [Tags("Stakeholder Management")]
    //[Authorize]
    public class StakeholdersController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        public StakeholdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Get paginated list of stakeholders for a project with optional search and sorting.
        /// </summary>
        [HttpGet]
        [Route("project/{projectId:guid}")]
        [ProducesResponseType(typeof(Result<PaginatedResult<GetStakeholdersResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStakeholders(Guid projectId, [FromQuery] GetStakeholdersQuery query)
        {
            var queryWithProject = query with { ProjectId = projectId };
            var result = await _mediator.Send(queryWithProject);
            return HandleResult(result);
        }

        /// <summary>
        /// Get a single stakeholder by ID.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(typeof(Result<GetStakeholderByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetStakeholderById(Guid id)
        {
            var result = await _mediator.Send(new GetStakeholderByIdQuery { Id = id });
            return HandleResult(result);
        }

        /// <summary>
        /// Create a new stakeholder for a project.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(Result<CreateStakeholderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateStakeholder([FromBody] CreateStakeholderCommand command)
        {
            var result = await _mediator.Send(command);
            return HandleResult(result);
        }

        /// <summary>
        /// Update an existing stakeholder. All fields are optional (partial update).
        /// </summary>
        [HttpPut("{id:guid}")]
        [ProducesResponseType(typeof(Result<UpdateStakeholderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateStakeholder(Guid id, [FromBody] UpdateStakeholderCommand command)
        {
            var updateCommand = command with { Id = id };
            var result = await _mediator.Send(updateCommand);
            return HandleResult(result);
        }

        /// <summary>
        /// Soft delete a stakeholder by ID.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [ProducesResponseType(typeof(Result<DeleteStakeholderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteStakeholder(Guid id)
        {
            var result = await _mediator.Send(new DeleteStakeholderCommand { Id = id });
            return HandleResult(result);
        }
    }
}

