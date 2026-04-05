using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.UniAssign.Commands.BulkEnterpriseAssignment;
using IOCv2.Application.Features.UniAssign.Commands.BulkReassignEnterprise;
using IOCv2.Application.Features.UniAssign.Commands.BulkUnassign;
using IOCv2.Application.Features.UniAssign.Commands.QuickEnterpriseAssignment;
using IOCv2.Application.Features.UniAssign.Commands.ReAssignSingle;
using IOCv2.Application.Features.UniAssign.Commands.UnAssignSingle;
using IOCv2.Application.Features.UniAssign.Queries.GetEnterpriseInterPhase;
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
    /// Bulk assign students to an enterprise intern phase (UniAssign).
    /// </summary>
    [HttpPost("bulk-enterprise-assignment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BulkEnterpriseAssignment([FromBody] BulkEnterpriseAssignmentCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bulk reassign students to a new enterprise intern phase (UniAssign reassign).
    /// </summary>
    [HttpPost("bulk-reassign-enterprise")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BulkReassignEnterprise([FromBody] BulkReassignEnterpriseCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bulk unassign students previously assigned via UniAssign.
    /// </summary>
    [HttpPost("bulk-unassign")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> BulkUnassign([FromBody] BulkUnassignCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Quickly assign a single student to an enterprise intern phase (UniAssign quick assign).
    /// </summary>
    [HttpPost("quick-enterprise-assignment")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> QuickEnterpriseAssignment([FromBody] QuickEnterpriseAssignmentCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Reassign a single student's application to a new enterprise/intern phase (UniAssign reassign single).
    /// </summary>
    [HttpPost("reassign-single")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReAssignSingle([FromBody] ReAssignSingleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Unassign a single student's application that was assigned via UniAssign.
    /// </summary>
    [HttpPost("unassign-single")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UnAssignSingle([FromBody] UnAssignSingleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get enterprise intern phases.
    /// </summary>
    [HttpGet("enterprise-interphases")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetEnterpriseInterPhases([FromQuery] string? searchTerm, CancellationToken cancellationToken)
    {
        var query = new GetEnterpriseInterPhaseQuery { SearchTerm = searchTerm };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}