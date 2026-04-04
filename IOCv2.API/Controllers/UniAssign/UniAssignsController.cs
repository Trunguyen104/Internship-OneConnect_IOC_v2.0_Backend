using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.UniAssign.Commands.QuickEnterpriseAssignment;
using IOCv2.Application.Features.UniAssign.Commands.ReAssignSingle;
using IOCv2.Application.Features.UniAssign.Commands.UnAssignSingle;
using IOCv2.Application.Features.UniAssign.Queries;
using IOCv2.Application.Features.UniAssign.Queries.GetEnterpriseInterPhase;
using IOCv2.Application.Features.HRApplications.UniAssign.Commands.ApproveUniAssign;
using IOCv2.Application.Features.HRApplications.UniAssign.Commands.RemovePlacedUniAssign;
using IOCv2.Application.Features.HRApplications.UniAssign.Queries.GetUniAssignApplications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.UniAssign;

[Authorize]
[Tags("Assignments")]
public class UniAssignsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public UniAssignsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Uni Admin quick assign: create a PendingAssignment for a single student (inline).
    /// </summary>
    [HttpPost("uni-assign")]
    [Authorize(Roles = "SchoolAdmin")]
    [ProducesResponseType(typeof(ApiResponse<QuickEnterpriseAssignmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateUniAssign([FromBody] QuickEnterpriseAssignmentCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get Enterprise Inter-Phase data.
    /// </summary>
    [HttpGet("enterprise-interphase")]
    [Authorize(Roles = "SchoolAdmin")]
    [ProducesResponseType(typeof(ApiResponse<List<GetEnterpriseInterPhaseResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetEnterpriseInterPhase([FromQuery] GetEnterpriseInterPhaseQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Enterprise: list uni-assign applications (paginated, filters).
    /// </summary>
    [HttpGet("uni-assign-applications")]
    [Authorize(Roles = "SchoolAdmin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetUniAssignApplicationsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUniAssignApplications([FromQuery] GetUniAssignApplicationsQuery query, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Enterprise approves a pending uni-assign (place student).
    /// </summary>
    [HttpPost("approve-uni-assign")]
    [Authorize(Roles = "SchoolAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ApproveUniAssignResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveUniAssign([FromBody] ApproveUniAssignCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Enterprise removes a placed uni-assign (HR remove placed student).
    /// </summary>
    [HttpPost("remove-placed-uni-assign")]
    [Authorize(Roles = "SchoolAdmin")]
    [ProducesResponseType(typeof(ApiResponse<RemovePlacedUniAssignResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemovePlacedUniAssign([FromBody] RemovePlacedUniAssignCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Uni Admin unassign: withdraw a pending or placed uni-assign.
    /// </summary>
    [HttpPost("unassign-single")]
    [Authorize(Roles = "SchoolAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UnAssignSingleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnAssignSingle([FromBody] UnAssignSingleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Uni Admin reassign: change enterprise for a student's uni-assign (supports pending -> update or placed -> withdraw+create pending).
    /// </summary>
    [HttpPost("reassign-single")]
    [Authorize(Roles = "SchoolAdmin")]
    [ProducesResponseType(typeof(ApiResponse<ReAssignSingleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReAssignSingle([FromBody] ReAssignSingleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}