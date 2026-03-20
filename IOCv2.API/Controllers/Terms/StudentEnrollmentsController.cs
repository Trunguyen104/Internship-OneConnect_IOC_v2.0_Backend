using Asp.Versioning;
using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StudentTerms.Commands.AddStudentManual;
using IOCv2.Application.Features.StudentTerms.Commands.BulkWithdrawStudents;
using IOCv2.Application.Features.StudentTerms.Commands.ImportStudentsConfirm;
using IOCv2.Application.Features.StudentTerms.Commands.ImportStudentsPreview;
using IOCv2.Application.Features.StudentTerms.Queries.DownloadImportTemplate;
using IOCv2.Application.Features.StudentTerms.Queries.GetStudents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Terms;

/// <summary>
/// Student Enrollments — Manage student enrollments within a term
/// </summary>
[Tags("Student Enrollments")]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/terms/{termId:guid}/enrollments")]
[Authorize(Roles = "SchoolAdmin,SuperAdmin")]
public class StudentEnrollmentsController : Controllers.ApiControllerBase
{
    private readonly IMediator _mediator;

    public StudentEnrollmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get paginated list of students enrolled in a term
    /// </summary>
    [HttpGet]
    [RateLimit(maxRequests: 60, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetStudentsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudents(
        [FromRoute] Guid termId,
        [FromQuery] GetStudentsQuery query,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(query with { TermId = termId }, cancellationToken));
    }

    /// <summary>
    /// Add a student manually to a term
    /// </summary>
    [HttpPost]
    [RateLimit(maxRequests: 30, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<AddStudentManualResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddStudentManual(
        [FromRoute] Guid termId,
        [FromBody] AddStudentManualCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { TermId = termId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Download Excel import template
    /// </summary>
    [HttpGet("template")]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    public async Task<IActionResult> DownloadTemplate(
        [FromRoute] Guid termId,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DownloadImportTemplateQuery(termId), cancellationToken);
        if (!result.IsSuccess)
            return HandleResult(result);

        return File(result.Data!.FileContent, result.Data.ContentType, result.Data.FileName);
    }

    /// <summary>
    /// Preview import from Excel file (validate only, no DB write)
    /// </summary>
    [HttpPost("import-preview")]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<ImportStudentsPreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportPreview(
        [FromRoute] Guid termId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var command = new ImportStudentsPreviewCommand { TermId = termId, File = file };
        return HandleResult(await _mediator.Send(command, cancellationToken));
    }

    /// <summary>
    /// Confirm import of valid records from preview
    /// </summary>
    [HttpPost("import-confirm")]
    [RateLimit(maxRequests: 10, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<ImportStudentsConfirmResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ImportConfirm(
        [FromRoute] Guid termId,
        [FromBody] ImportStudentsConfirmCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(command with { TermId = termId }, cancellationToken));
    }

    /// <summary>
    /// Bulk withdraw multiple students from a term
    /// </summary>
    [HttpPatch("bulk-withdraw")]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<BulkWithdrawStudentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BulkWithdraw(
        [FromRoute] Guid termId,
        [FromBody] BulkWithdrawStudentsCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(command with { TermId = termId }, cancellationToken));
    }
}
