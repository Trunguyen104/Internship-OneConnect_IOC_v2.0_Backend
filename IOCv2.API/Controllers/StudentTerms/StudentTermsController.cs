using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StudentTerms.Commands.RestoreStudent;
using IOCv2.Application.Features.StudentTerms.Commands.UpdateStudentTerm;
using IOCv2.Application.Features.StudentTerms.Commands.WithdrawStudent;
using IOCv2.Application.Features.StudentTerms.Queries.GetStudentTermDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.StudentTerms;

/// <summary>
/// Student Term Detail Management — Manage individual enrollment records
/// </summary>
[Tags("Student Term Detail")]
[Route("api/v{version:apiVersion}/student-terms")]
[Authorize(Roles = "SchoolAdmin,SuperAdmin")]
public class StudentTermsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public StudentTermsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get detailed enrollment information for a specific student-term record
    /// </summary>
    [HttpGet("{id:guid}", Name = "GetStudentTermDetail")]
    [RateLimit(maxRequests: 60, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<GetStudentTermDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetStudentTermDetail(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new GetStudentTermDetailQuery(id), cancellationToken));
    }

    /// <summary>
    /// Update enrollment and student profile information
    /// </summary>
    [HttpPut("{id:guid}")]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<UpdateStudentTermResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> UpdateStudentTerm(
        [FromRoute] Guid id,
        [FromBody] UpdateStudentTermCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(command with { StudentTermId = id }, cancellationToken));
    }

    /// <summary>
    /// Withdraw a single student from a term (must be Unplaced)
    /// </summary>
    [HttpPatch("{id:guid}/withdraw")]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<WithdrawStudentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> WithdrawStudent(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new WithdrawStudentCommand(id), cancellationToken));
    }

    /// <summary>
    /// Restore a previously withdrawn student back to Active/Unplaced
    /// </summary>
    [HttpPatch("{id:guid}/restore")]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<RestoreStudentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> RestoreStudent(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new RestoreStudentCommand(id), cancellationToken));
    }

}
