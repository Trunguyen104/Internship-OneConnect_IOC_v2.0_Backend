using Asp.Versioning;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentDetail;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentEvaluations;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentList;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbook;
using IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentViolations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.UniAdminInternship;

[Tags("Uni-Admin - Internship Monitoring")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/uni-admin/internship/students")]
[Authorize(Roles = "SchoolAdmin")]
public class UniAdminInternshipController : Controllers.ApiControllerBase
{
    private readonly IMediator _mediator;

    public UniAdminInternshipController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<GetUniAdminStudentListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetStudentList(
        [FromQuery] GetUniAdminStudentListQuery query,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("{studentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<GetUniAdminStudentDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentDetail(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? termId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUniAdminStudentDetailQuery
        {
            StudentId = studentId,
            TermId = termId
        };

        return HandleResult(await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("{studentId:guid}/logbook")]
    [ProducesResponseType(typeof(ApiResponse<GetUniAdminStudentLogbookResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentLogbook(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? termId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 4,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUniAdminStudentLogbookQuery
        {
            StudentId = studentId,
            TermId = termId,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        return HandleResult(await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("{studentId:guid}/evaluations")]
    [ProducesResponseType(typeof(ApiResponse<GetUniAdminStudentEvaluationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentEvaluations(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? termId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUniAdminStudentEvaluationsQuery
        {
            StudentId = studentId,
            TermId = termId
        };

        return HandleResult(await _mediator.Send(query, cancellationToken));
    }

    [HttpGet("{studentId:guid}/violations")]
    [ProducesResponseType(typeof(ApiResponse<GetUniAdminStudentViolationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentViolations(
        [FromRoute] Guid studentId,
        [FromQuery] Guid? termId,
        CancellationToken cancellationToken = default)
    {
        var query = new GetUniAdminStudentViolationsQuery
        {
            StudentId = studentId,
            TermId = termId
        };

        return HandleResult(await _mediator.Send(query, cancellationToken));
    }
}

