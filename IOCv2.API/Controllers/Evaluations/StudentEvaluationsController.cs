using IOCv2.Application.Features.StudentEvaluations.Queries.GetMyEvaluationDetail;
using IOCv2.Application.Features.StudentEvaluations.Queries.GetStudentEvaluationCycles;
using IOCv2.Application.Features.StudentEvaluations.Queries.GetStudentTeamEvaluations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers.Evaluations;

/// <summary>
/// Quản lý điểm số và danh sách đợt đánh giá góc nhìn sinh viên.
/// </summary>
[Tags("student-evaluations")]
[Authorize(Roles = "Student,SuperAdmin")]
[Route("api/students/me")]
public class StudentEvaluationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public StudentEvaluationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Lấy danh sách các đợt đánh giá áp dụng cho kỳ thực tập hiện tại của sinh viên.
    /// </summary>
    [HttpGet("internships/{internshipId:guid}/evaluation-cycles")]
    [ProducesResponseType(typeof(List<GetStudentEvaluationCyclesResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentEvaluationCycles(
        [FromRoute] Guid internshipId,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var roleStr = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;

        var query = new GetStudentEvaluationCyclesQuery 
        { 
            InternshipId = internshipId, 
            CurrentUserId = userId,
            Role = roleStr
        };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Xem bảng điểm tổng quan của nhóm thuộc chu kỳ đánh giá (Privacy guard áp dụng cho điểm người khác).
    /// </summary>
    [HttpGet("evaluation-cycles/{cycleId:guid}/team-evaluations")]
    [ProducesResponseType(typeof(List<GetStudentTeamEvaluationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStudentTeamEvaluations(
        [FromRoute] Guid cycleId,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var roleStr = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;

        var query = new GetStudentTeamEvaluationsQuery 
        { 
            CycleId = cycleId, 
            CurrentUserId = userId,
            Role = roleStr
        };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Xem chi tiết phiếu điểm cá nhân của sinh viên trong một chu kỳ đánh giá.
    /// Cho phép xem barem mẫu (Rubric) ngay cả khi Mentor chưa chấm điểm.
    /// </summary>
    [HttpGet("evaluation-cycles/{cycleId:guid}/my-evaluation")]
    [ProducesResponseType(typeof(GetMyEvaluationDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMyEvaluationDetail(
        [FromRoute] Guid cycleId,
        CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var roleStr = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;

        var query = new GetMyEvaluationDetailQuery 
        { 
            CycleId = cycleId, 
            CurrentUserId = userId,
            Role = roleStr
        };
        var result = await _mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}
