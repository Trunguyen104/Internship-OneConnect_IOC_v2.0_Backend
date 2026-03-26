using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Jobs.Commands.ApplyJob;
using IOCv2.Application.Features.Jobs.Commands.CloseJob;
using IOCv2.Application.Features.Jobs.Commands.CreateJobPosting;
using IOCv2.Application.Features.Jobs.Commands.DeleteJob;
using IOCv2.Application.Features.Jobs.Commands.PublishJob;
using IOCv2.Application.Features.Jobs.Commands.UpdateJob;
using IOCv2.Application.Features.Jobs.Queries.GetJobById;
using IOCv2.Application.Features.Jobs.Queries.GetJobs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.API.Controllers.Jobs
{
    /// <summary>
    /// Jobs / Job Postings controller.
    /// Exposes endpoints for listing, viewing, creating and managing job postings.
    /// - Student-facing endpoints (apply) and HR/Enterprise-facing endpoints (create/publish/update/close/delete).
    /// </summary>
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
        /// Get paginated list of job postings.
        /// - HR: can filter by status and include deleted (default off)
        /// - Students/Universities: only see published jobs (and respecting audience)
        /// </summary>
        /// <param name="query">Query parameters (search, pagination, filters)</param>
        [HttpGet]
        [Authorize(Roles = "HR,Student")]
        [RateLimit(maxRequests: 60, windowMinutes: 1)]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetJobsResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetJobs(
            [FromQuery] GetJobsQuery query,
            CancellationToken cancellationToken = default)
        {
            // Delegate to application layer
            return HandleResult(await _mediator.Send(query, cancellationToken));
        }

        /// <summary>
        /// Get job posting details by id.
        /// </summary>
        /// <param name="id">Job id</param>
        [HttpGet("{id:guid}", Name = "GetJobById")]
        [Authorize(Roles = "HR,Student")]
        [RateLimit(maxRequests: 60, windowMinutes: 1)]
        [ProducesResponseType(typeof(ApiResponse<GetJobByIdResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> GetJobById(
            [FromRoute] Guid id,
            CancellationToken cancellationToken = default)
        {
            return HandleResult(await _mediator.Send(new GetJobByIdQuery { JobId = id }, cancellationToken));
        }

        /// <summary>
        /// Create a new job posting (HR / Enterprise).
        /// Returns 201 Created with location header to GetJobById.
        /// </summary>
        /// <param name="command">Create job command</param>
        [HttpPost]
        [Authorize(Roles = "HR")]
        [RateLimit(maxRequests: 20, windowMinutes: 1)]
        [ProducesResponseType(typeof(ApiResponse<CreateJobPostingResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateJobPosting(
            [FromBody] CreateJobPostingCommand command,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(command, cancellationToken);
            // CreatedAtAction -> GetJobById
            return HandleCreateResult(result, nameof(GetJobById), new { id = result.Data?.JobId, version = "1" });
        }

        /// <summary>
        /// Student applies to a job posting.
        /// - Students call this endpoint to submit an application.
        /// </summary>
        /// <param name="id">Job id</param>
        /// <param name="command">Apply command (body)</param>
        [HttpPost("{id:guid}/apply")]
        [Authorize(Roles = "Student")]
        [RateLimit(maxRequests: 30, windowMinutes: 1)]
        [ProducesResponseType(typeof(ApiResponse<ApplyJobResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ApplyJob(
            [FromRoute] Guid id,
            [FromBody] ApplyJobCommand command,
            CancellationToken cancellationToken = default)
        {
            // ensure JobId is set from route - frontend may omit it
            var result = await _mediator.Send(command with { JobId = id }, cancellationToken);
            return HandleCreateResult(result, nameof(GetJobById), new { id = result.Data?.ApplicationId, version = "1" });
        }

        /// <summary>
        /// Publish a job posting (HR).
        /// </summary>
        /// <param name="id">Job id</param>
        [HttpPost("{id:guid}/publish")]
        [Authorize(Roles = "HR")]
        [RateLimit(maxRequests: 20, windowMinutes: 1)]
        [ProducesResponseType(typeof(ApiResponse<PublishJobResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> PublishJob(
            [FromRoute] Guid id,
            [FromBody] PublishJobCommand command,
            CancellationToken cancellationToken = default)
        {
            // command may contain additional options; ensure JobId comes from route
            return HandleResult(await _mediator.Send(command with { JobId = id }, cancellationToken));
        }

        /// <summary>
        /// Update an existing job posting.
        /// - HR edits Job (Draft / Published / Closed).
        /// - For Published jobs with applications, frontend must present confirmation and set ForceUpdateWithApplications when user confirms.
        /// </summary>
        /// <param name="id">Job id</param>
        /// <param name="command">Updated job data</param>
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "HR")]
        [RateLimit(maxRequests: 20, windowMinutes: 1)]
        [ProducesResponseType(typeof(ApiResponse<UpdateJobResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> UpdateJob(
            [FromRoute] Guid id,
            [FromBody] UpdateJobCommand command,
            CancellationToken cancellationToken = default)
        {
            // attach route id to command
            return HandleResult(await _mediator.Send(command with { JobId = id }, cancellationToken));
        }

        /// <summary>
        /// Close a published job posting.
        /// - If active applications exist the command may include confirmation flag.
        /// </summary>
        /// <param name="id">Job id</param>
        /// <param name="command">Close job command</param>
        [HttpPatch("{id:guid}/close")]
        [Authorize(Roles = "HR")]
        [RateLimit(maxRequests: 20, windowMinutes: 1)]
        [ProducesResponseType(typeof(ApiResponse<CloseJobResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CloseJob(
            [FromRoute] Guid id,
            [FromBody] CloseJobCommand command,
            CancellationToken cancellationToken)
        {
            return HandleResult(await _mediator.Send(command with { JobId = id }, cancellationToken));
        }

        /// <summary>
        /// Soft-delete a job posting.
        /// - If active applications exist the command may include confirmation flag.
        /// - Deleted jobs are hidden from normal lists unless IncludeDeleted is enabled.
        /// </summary>
        /// <param name="id">Job id</param>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "HR")]
        [RateLimit(maxRequests: 20, windowMinutes: 1)]
        [ProducesResponseType(typeof(ApiResponse<DeleteJobResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteJob(
            [FromRoute] Guid id,
            [FromBody] DeleteJobCommand command,
            CancellationToken cancellationToken)
        {
            return HandleResult(await _mediator.Send(command with { JobId = id }, cancellationToken));
        }
    }
}
