using IOCv2.API.Attributes;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.StudentTerms.Commands.AddStudentManual;
using IOCv2.Application.Features.StudentTerms.Commands.BulkWithdrawStudents;
using IOCv2.Application.Features.StudentTerms.Commands.ImportStudents;
using IOCv2.Application.Features.StudentTerms.Queries.DownloadImportTemplate;
using IOCv2.Application.Features.StudentTerms.Queries.GetStudents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.StudentTerms;

/// <summary>
/// Student Enrollment Management — Manage student enrollments within an internship term
/// </summary>
[Tags("Student Enrollment Management")]
[Authorize(Roles = "SchoolAdmin,SuperAdmin")]
public class StudentEnrollmentsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public StudentEnrollmentsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Get a paginated list of students enrolled in a term
    /// </summary>
    [HttpGet("~/api/v{version:apiVersion}/terms/{termId:guid}/enrollments")]
    [RateLimit(maxRequests: 60, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<GetStudentsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> GetStudents(
        [FromRoute] Guid termId,
        [FromQuery] GetStudentsQuery query,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(query with { TermId = termId }, cancellationToken));
    }

    /// <summary>
    /// Add a single student manually to a term (creates an account if not exists)
    /// </summary>
    [HttpPost("~/api/v{version:apiVersion}/terms/{termId:guid}/enrollments")]
    [RateLimit(maxRequests: 20, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<AddStudentManualResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> AddStudentManual(
        [FromRoute] Guid termId,
        [FromBody] AddStudentManualCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { TermId = termId }, cancellationToken);
        if (!result.IsSuccess) return HandleResult(result);
        return StatusCode(StatusCodes.Status201Created,
            new ApiResponse<AddStudentManualResponse>(true, result.Message ?? "Created successfully", result.Data!));
    }

    /// <summary>
    /// Download the Excel import template
    /// </summary>
    [HttpGet("~/api/v{version:apiVersion}/terms/{termId:guid}/enrollments/template")]
    [RateLimit(maxRequests: 30, windowMinutes: 1)]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new DownloadImportTemplateQuery(), cancellationToken);
        if (!result.IsSuccess || result.Data == null) return HandleResult(result);
        return File(result.Data.FileContent, result.Data.ContentType, result.Data.FileName);
    }

    /// <summary>
    /// Preview the Excel import file — validates rows without writing to DB
    /// </summary>
    [HttpPost("~/api/v{version:apiVersion}/terms/{termId:guid}/enrollments/import-preview")]
    [RateLimit(maxRequests: 5, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<ImportStudentsPreviewResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ImportPreview(
        [FromRoute] Guid termId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        var command = new ImportStudentsPreviewCommand { TermId = termId, File = file };
        return HandleResult(await _mediator.Send(command, cancellationToken));
    }

    /// <summary>
    /// Confirm import — enroll valid students and return a password file for new accounts
    /// </summary>
    [HttpPost("~/api/v{version:apiVersion}/terms/{termId:guid}/enrollments/import-confirm")]
    [RateLimit(maxRequests: 5, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<ImportStudentsConfirmResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ImportConfirm(
        [FromRoute] Guid termId,
        [FromBody] ImportStudentsConfirmCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(command with { TermId = termId }, cancellationToken);
        if (!result.IsSuccess) return HandleResult(result);

        var data = result.Data!;

        // If there is a password file, return it as a downloadable Excel file
        if (data.PasswordFileContent != null && data.PasswordFileContent.Length > 0)
        {
            Response.Headers.Append("X-Imported-Count", data.ImportedCount.ToString());
            Response.Headers.Append("X-Skipped-Count", data.SkippedCount.ToString());
            Response.Headers.Append("X-Import-Message", Uri.EscapeDataString(result.Message ?? ""));
            return File(data.PasswordFileContent,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                data.PasswordFileFileName ?? "student_passwords.xlsx");
        }

        // No new accounts created — return normal JSON
        return HandleResult(result);
    }

    /// <summary>
    /// Bulk withdraw multiple Unplaced students from a term
    /// </summary>
    [HttpPatch("~/api/v{version:apiVersion}/terms/{termId:guid}/enrollments/bulk-withdraw")]
    [RateLimit(maxRequests: 10, windowMinutes: 1)]
    [ProducesResponseType(typeof(ApiResponse<BulkWithdrawStudentsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> BulkWithdraw(
        [FromRoute] Guid termId,
        [FromBody] BulkWithdrawStudentsCommand command,
        CancellationToken cancellationToken = default)
    {
        return HandleResult(await _mediator.Send(command with { TermId = termId }, cancellationToken));
    }
}