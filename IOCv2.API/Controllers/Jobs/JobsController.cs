using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Jobs.Commands.CreateJobPosting;
using IOCv2.Application.Features.Jobs.Queries.GetJobs;
using IOCv2.Application.Features.Jobs.Queries.GetJobById;
using IOCv2.Application.Features.Jobs.Commands.ApplyJob;
using IOCv2.Application.Features.Jobs.Commands.PublishJob;
using IOCv2.Application.Features.Jobs.Commands.UpdateJob;
using IOCv2.Application.Features.Jobs.Commands.CloseJob;
using IOCv2.Application.Features.Jobs.Commands.DeleteJob;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using System;

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
        /// Create a job posting (saves as Draft). Only HR users may call this endpoint.
        /// AC-01: Save Draft -> returns 201 Created with created DTO and message "Đã lưu bản nháp."
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "HR")]
        [ProducesResponseType(typeof(ApiResponse<CreateJobPostingResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateJobPosting([FromBody] CreateJobPostingCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);

            if (result != null && result.IsSuccess && result.Data != null)
            {
                // Return Created pointing to GetJobById
                return CreatedAtAction(nameof(GetJobById), new { id = result.Data.JobId }, new ApiResponse<CreateJobPostingResponse>(true, result.Message ?? "Đã lưu bản nháp.", result.Data));
            }

            return HandleResult(result);
        }

        /// <summary>
        /// Publish a job posting (Draft -> Published). Only HR users may call this endpoint.
        /// AC-02: If deadline passed -> block with message; otherwise set status to Published and return success message.
        /// </summary>
        [HttpPost("{id}/publish")]
        [Authorize(Roles = "HR")]
        [ProducesResponseType(typeof(ApiResponse<PublishJobResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> PublishJob(Guid id, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(new PublishJobCommand { JobId = id }, cancellationToken);

            if (result != null && result.IsSuccess && result.Data != null)
            {
                return Ok(new ApiResponse<PublishJobResponse>(true, result.Message ?? "Job Posting đã được đăng tuyển.", result.Data));
            }

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

        /// <summary>
        /// Update a job posting (Draft/Published/Closed). HR only.
        /// If the job is Published and has applications, the first call should be made with ConfirmWhenHasApplications = false
        /// to receive a warning. If the HR confirms, call again with ConfirmWhenHasApplications = true to apply changes.
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "HR")]
        [ProducesResponseType(typeof(ApiResponse<UpdateJobResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobCommand command, CancellationToken cancellationToken)
        {
            command.JobId = id;
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Close a published job posting. HR only.
        /// If the job has active applications, the first call without confirmation returns a warning.
        /// Call again with ConfirmWhenHasActiveApplications = true to confirm and apply the change.
        /// </summary>
        [HttpPost("{id}/close")]
        [Authorize(Roles = "HR")]
        [ProducesResponseType(typeof(ApiResponse<CloseJobResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CloseJob(Guid id, [FromBody] CloseJobCommand command, CancellationToken cancellationToken)
        {
            command = command with { JobId = id };
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Soft-delete a job posting. HR only.
        /// If the job has active applications, the first call without confirmation returns a warning.
        /// Call again with ConfirmWhenHasActiveApplications = true to confirm and perform deletion.
        /// </summary>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "HR")]
        [ProducesResponseType(typeof(ApiResponse<DeleteJobResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteJob([FromRoute] Guid id, [FromBody] DeleteJobCommand command, CancellationToken cancellationToken)
        {
            command = command with { JobId = id };
            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }
    }
}
