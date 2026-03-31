using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipPhases.Commands.CreateInternshipPhase;
using IOCv2.Application.Features.InternshipPhases.Commands.DeleteInternshipPhase;
using IOCv2.Application.Features.InternshipPhases.Commands.UpdateInternshipPhase;
using IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhaseById;
using IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhases;
using IOCv2.Application.Features.InternshipPhases.Queries.GetMyInternshipPhases;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.InternshipPhases;

/// <summary>
/// Internship Phases Management — manage internship phases owned by enterprises.
/// </summary>
[Tags("Internship Phases")]
[Route("api/v{version:apiVersion}/internship-phases")]
[Authorize]
public class InternshipPhasesController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public InternshipPhasesController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get paginated list of internship phases for an enterprise.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,HR,EnterpriseAdmin")]
    [RateLimit(maxRequests: 60, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetInternshipPhasesResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetInternshipPhases(
        [FromQuery] GetInternshipPhasesQuery query,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(query, cancellationToken));
    }

    /// <summary>
    /// Get details of an internship phase by ID.
    /// </summary>
    [HttpGet("{phaseId:guid}", Name = "GetInternshipPhaseById")]
    [Authorize(Roles = "SuperAdmin,SchoolAdmin,HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<GetInternshipPhaseByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetInternshipPhaseById(
        [FromRoute] Guid phaseId,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new GetInternshipPhaseByIdQuery(phaseId), cancellationToken));
    }

    /// <summary>
    /// Get internship phases that the current user belongs to.
    /// Student: phases via their internship groups.
    /// Mentor: phases via the groups they mentor.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = "Student,Mentor")]
    [RateLimit(maxRequests: 60, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<List<GetMyInternshipPhasesResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyInternshipPhases(
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new GetMyInternshipPhasesQuery(), cancellationToken));
    }

    /// <summary>
    /// Create a new internship phase for an enterprise.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [RateLimit(maxRequests: 60, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<CreateInternshipPhaseResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> CreateInternshipPhase(
        [FromBody] CreateInternshipPhaseCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetInternshipPhaseById), new { phaseId = result.Data?.PhaseId, version = "1" });
    }

    /// <summary>
    /// Update an internship phase. Cannot update if status is Closed.
    /// </summary>
    [HttpPut("{phaseId:guid}")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [ProducesResponseType(typeof(ApiResponse<UpdateInternshipPhaseResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateInternshipPhase(
        [FromRoute] Guid phaseId,
        [FromBody] UpdateInternshipPhaseCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(command with { PhaseId = phaseId }, cancellationToken));
    }

    /// <summary>
    /// Soft delete an internship phase. Cannot delete if there are active internship groups.
    /// </summary>
    [HttpDelete("{phaseId:guid}")]
    [Authorize(Roles = "HR,EnterpriseAdmin")]
    [RateLimit(maxRequests: 60, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DeleteInternshipPhase(
        [FromRoute] Guid phaseId,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new DeleteInternshipPhaseCommand(phaseId), cancellationToken));
    }
}
