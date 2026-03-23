using Asp.Versioning;
using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StudentTerms.Commands.RestoreStudent;
using IOCv2.Application.Features.StudentTerms.Commands.UpdateStudentTerm;
using IOCv2.Application.Features.StudentTerms.Commands.WithdrawStudent;
using IOCv2.Application.Features.StudentTerms.Queries.GetStudentTermDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Terms;

/// <summary>
/// Student Terms — Manage individual student term records
/// </summary>
[Tags("Student Terms")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/student-terms")]
[Authorize(Roles = "SchoolAdmin,SuperAdmin,HR,Mentor,EnterpriseAdmin")]
public class StudentTermsController : Controllers.ApiControllerBase
{
    private readonly IMediator _mediator;

    public StudentTermsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get detailed information of a student term enrollment record
    /// </summary>
    [HttpGet("{id:guid}")]
    [RateLimit(maxRequests: 60, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<GetStudentTermDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudentTermDetail(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new GetStudentTermDetailQuery(id), cancellationToken));
    }

    /// <summary>
    /// Update student profile and enrollment information
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "SchoolAdmin,SuperAdmin")]
    [RateLimit(maxRequests: 30, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<UpdateStudentTermResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateStudentTerm(
        [FromRoute] Guid id,
        [FromBody] UpdateStudentTermCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(command with { StudentTermId = id }, cancellationToken));
    }

    /// <summary>
    /// Withdraw a student from their term enrollment (must be Active and Unplaced)
    /// </summary>
    [HttpPatch("{id:guid}/withdraw")]
    [Authorize(Roles = "SchoolAdmin,SuperAdmin")]
    [RateLimit(maxRequests: 30, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<WithdrawStudentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> WithdrawStudent(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new WithdrawStudentCommand(id), cancellationToken));
    }

    /// <summary>
    /// Restore a withdrawn student back to Active/Unplaced (term must still be Open)
    /// </summary>
    [HttpPatch("{id:guid}/restore")]
    [Authorize(Roles = "SchoolAdmin,SuperAdmin")]
    [RateLimit(maxRequests: 30, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<RestoreStudentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreStudent(
        [FromRoute] Guid id,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(new RestoreStudentCommand(id), cancellationToken));
    }
}
