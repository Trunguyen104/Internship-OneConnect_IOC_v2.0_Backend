using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.UniAssign.Commands.QuickEnterpriseAssignment;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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
}