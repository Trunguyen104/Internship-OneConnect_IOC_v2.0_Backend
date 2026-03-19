using System;
using System.Threading;
using System.Threading.Tasks;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.ViolationReports.Commands.CreateViolationReport;
using IOCv2.Application.Features.ViolationReports.Commands.UpdateViolationReport;
using IOCv2.Application.Features.ViolationReports.Commands.DeleteViolationReport;
using IOCv2.Application.Features.ViolationReports.Queries.GetViolationReportDetail;
using IOCv2.Application.Features.ViolationReports.Queries.GetViolationReports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.ViolationReports
{
    /// <summary>
    /// Controller responsible for handling Violation Report CRUD endpoints.
    /// Requires authentication for all actions; method-level role restrictions are applied where needed.
    /// </summary>
    [ApiController]
    [Authorize]
    public class ViolationReportsController : ApiControllerBase
    {
        private readonly IMediator _mediator;

        /// <summary>
        /// Constructs the controller with required dependencies.
        /// </summary>
        /// <param name="mediator">MediatR mediator used to send commands/queries to the application layer.</param>
        public ViolationReportsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Retrieves a paginated list of violation reports.
        /// Accessible to users in the Student and Mentor roles.
        /// </summary>
        /// <param name="query">Query parameters for paging, filtering and sorting.</param>
        /// <param name="cancellationToken">Cancellation token propagated to mediator.</param>
        /// <returns>Paginated result wrapped in ApiResponse on success.</returns>
        [HttpGet]
        [Authorize(Roles = "Student,Mentor")]
        [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetViolationReportsResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetViolationReports([FromQuery] GetViolationReportsQuery query, CancellationToken cancellationToken)
        {
            // Delegate query handling to application layer via MediatR.
            var result = await _mediator.Send(query, cancellationToken);

            // ApiControllerBase.HandleResult maps application result to appropriate IActionResult.
            return HandleResult(result);
        }

        /// <summary>
        /// Retrieves detail for a specific violation report by id.
        /// Accessible to users in the Student and Mentor roles.
        /// </summary>
        /// <param name="id">Violation report identifier (GUID).</param>
        /// <param name="cancellationToken">Cancellation token propagated to mediator.</param>
        /// <returns>Detailed violation report wrapped in ApiResponse on success.</returns>
        [HttpGet("{id:guid}")]
        [Authorize(Roles = "Student,Mentor")]
        [ProducesResponseType(typeof(ApiResponse<GetViolationReportDetailResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetViolationReportDetail([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            // Build query object explicitly to avoid coupling route parameter to query binding.
            var query = new GetViolationReportDetailQuery
            {
                ViolationReportId = id
            };

            var result = await _mediator.Send(query, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Creates a new violation report.
        /// Only users in the Mentor role can create reports.
        /// </summary>
        /// <param name="command">Create command containing payload for the new violation report.</param>
        /// <param name="cancellationToken">Cancellation token propagated to mediator.</param>
        /// <returns>Created resource location via 201 Created and the created DTO on success.</returns>
        [HttpPost]
        [Authorize(Roles = "Mentor")]
        [ProducesResponseType(typeof(ApiResponse<CreateViolationReportResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateViolationReport([FromBody] CreateViolationReportCommand command, CancellationToken cancellationToken)
        {
            // Forward create command to application layer.
            var result = await _mediator.Send(command, cancellationToken);

            // HandleCreateResult will produce a 201 Created with Location header pointing to GetViolationReportDetail.
            // The route values include the created ViolationReportId and API version.
            return HandleCreateResult(result, nameof(GetViolationReportDetail), new { id = result.Data?.ViolationReportId, Version = "1" });
        }

        /// <summary>
        /// Updates an existing violation report.
        /// Only users in the Mentor role can update reports.
        /// </summary>
        /// <param name="violationReportId">Identifier of the violation report to update.</param>
        /// <param name="command">Update command with new values.</param>
        /// <param name="cancellationToken">Cancellation token propagated to mediator.</param>
        /// <returns>Updated DTO wrapped in ApiResponse on success.</returns>
        [HttpPut("{violationReportId:guid}")]
        [Authorize(Roles = "Mentor")]
        [ProducesResponseType(typeof(ApiResponse<UpdateViolationReportResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateViolationReport([FromRoute] Guid violationReportId, [FromBody] UpdateViolationReportCommand command, CancellationToken cancellationToken)
        {
            // Ensure route id is applied to command to prevent mismatch between route and body.
            command.ViolationReportId = violationReportId;

            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }

        /// <summary>
        /// Deletes a violation report by id.
        /// Only users in the Mentor role can delete reports.
        /// </summary>
        /// <param name="id">Identifier of the violation report to delete.</param>
        /// <param name="cancellationToken">Cancellation token propagated to mediator.</param>
        /// <returns>Result of deletion wrapped in ApiResponse on success.</returns>
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Mentor")]
        [ProducesResponseType(typeof(ApiResponse<DeleteViolationReportResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteViolationReport([FromRoute] Guid id, CancellationToken cancellationToken)
        {
            // Map route id to delete command and dispatch.
            var command = new DeleteViolationReportCommand
            {
                ViolationReportId = id
            };

            var result = await _mediator.Send(command, cancellationToken);
            return HandleResult(result);
        }
    }
}
