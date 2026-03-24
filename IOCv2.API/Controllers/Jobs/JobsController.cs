using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Jobs.Queries.GetJobs;
using IOCv2.Application.Features.Jobs.Queries.GetJobById;
using IOCv2.Application.Features.Jobs.Commands.ApplyJob;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IOCv2.API.Controllers.Jobs
{
    [Authorize]
    [Tags("Jobs")]
    public class JobsController : ApiControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<JobsController> _logger;

        public JobsController(IMediator mediator, ILogger<JobsController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Get list of open job postings for the student's university.
        /// If the student is placed, the request returns Forbidden with a message indicating placement.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetJobsResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetJobs([FromQuery] GetJobsQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Get job details by id. Response includes fields required by AC-02 and whether the "Apply" button should be enabled.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<GetJobByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetJobById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new GetJobByIdQuery(id), cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Apply to a job using the student's current CV (snapshot saved).
        /// Only users in the Student role may call this endpoint.
        /// </summary>
        [HttpPost("{id}/apply")]
        [Authorize(Roles = "Student")]
        [ProducesResponseType(typeof(ApiResponse<ApplyJobResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ApplyToJob(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new ApplyJobCommand(id), cancellationToken);
            if (result != null && result.IsSuccess && result.Data != null)
            {
                // Return Created with message using existing convention
                return CreatedAtAction(nameof(GetJobById), new { id }, new ApiResponse<ApplyJobResponse>(true, result.Message ?? "Apply successful", result.Data));
            }

            return HandleResult(result);
        }
    }
}
