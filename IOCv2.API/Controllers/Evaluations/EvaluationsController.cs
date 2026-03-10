using IOCv2.Application.Features.EvaluationCycles.Commands.CreateEvaluationCycle;
using IOCv2.Application.Features.EvaluationCycles.Commands.DeleteEvaluationCycle;
using IOCv2.Application.Features.EvaluationCycles.Commands.UpdateEvaluationCycle;
using IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycleById;
using IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycles;
using IOCv2.Application.Features.EvaluationCriteria.Commands.CreateEvaluationCriteria;
using IOCv2.Application.Features.EvaluationCriteria.Commands.DeleteEvaluationCriteria;
using IOCv2.Application.Features.EvaluationCriteria.Commands.UpdateEvaluationCriteria;
using IOCv2.Application.Features.EvaluationCriteria.Queries.GetEvaluationCriteria;
using IOCv2.Application.Features.Evaluations.Commands.SaveEvaluations;
using IOCv2.Application.Features.Evaluations.Commands.UpdateEvaluation;
using IOCv2.Application.Features.Evaluations.Commands.SubmitEvaluation;
using IOCv2.Application.Features.Evaluations.Commands.PublishEvaluation;
using IOCv2.Application.Features.Evaluations.Queries.GetInternshipEvaluations;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

/// <summary>
/// Quản lý chu kỳ đánh giá, tiêu chí và điểm đánh giá.
/// </summary>
[Tags("evaluations")]
[Authorize]
[Route("api/evaluations")]
public class EvaluationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public EvaluationsController(IMediator mediator) => _mediator = mediator;

    // ==========================================
    // EVALUATION CYCLES
    // ==========================================

    /// <summary>
    /// Lấy danh sách chu kỳ đánh giá theo học kỳ.
    /// </summary>
    [HttpGet("cycles")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvaluationCycles([FromQuery] Guid termId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEvaluationCyclesQuery { TermId = termId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Lấy chi tiết một chu kỳ đánh giá kèm danh sách tiêu chí.
    /// </summary>
    [HttpGet("cycles/{cycleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvaluationCycleById(Guid cycleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEvaluationCycleByIdQuery(cycleId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Tạo chu kỳ đánh giá mới.
    /// </summary>
    [HttpPost("cycles")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEvaluationCycle(
        [FromBody] CreateEvaluationCycleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreatedResult(result);
    }

    /// <summary>
    /// Cập nhật thông tin chu kỳ đánh giá.
    /// </summary>
    [HttpPut("cycles/{cycleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvaluationCycle(
        Guid cycleId,
        [FromBody] UpdateEvaluationCycleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { CycleId = cycleId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Xóa chu kỳ đánh giá (soft delete). Không thể xóa nếu đã có tiêu chí.
    /// </summary>
    [HttpDelete("cycles/{cycleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvaluationCycle(Guid cycleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteEvaluationCycleCommand(cycleId), cancellationToken);
        return HandleResult(result);
    }

    // ==========================================
    // EVALUATION CRITERIA
    // ==========================================

    /// <summary>
    /// Lấy danh sách tiêu chí của một chu kỳ đánh giá.
    /// </summary>
    [HttpGet("criteria")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvaluationCriteria([FromQuery] Guid cycleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEvaluationCriteriaQuery(cycleId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Thêm tiêu chí mới vào chu kỳ đánh giá.
    /// </summary>
    [HttpPost("criteria")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEvaluationCriteria(
        [FromBody] CreateEvaluationCriteriaCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreatedResult(result);
    }

    /// <summary>
    /// Cập nhật tiêu chí đánh giá.
    /// </summary>
    [HttpPut("criteria/{criteriaId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvaluationCriteria(
        Guid criteriaId,
        [FromBody] UpdateEvaluationCriteriaCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { CriteriaId = criteriaId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Xóa tiêu chí đánh giá (soft delete).
    /// </summary>
    [HttpDelete("criteria/{criteriaId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvaluationCriteria(Guid criteriaId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteEvaluationCriteriaCommand(criteriaId), cancellationToken);
        return HandleResult(result);
    }

    // ==========================================
    // EVALUATIONS (Mentor grading)
    // ==========================================

    /// <summary>
    /// Lấy danh sách đánh giá của một nhóm thực tập trong một chu kỳ (Grid View).
    /// </summary>

    [HttpGet("cycles/{cycleId:guid}/internships/{internshipId:guid}/evaluations")]
    [ProducesResponseType(typeof(GetInternshipEvaluationsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInternshipEvaluations(
        [FromRoute] Guid cycleId,
        [FromRoute] Guid internshipId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetInternshipEvaluationsQuery { CycleId = cycleId, InternshipId = internshipId },
            cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Upsert (Lưu mới/Cập nhật) đánh giá cho một hoặc nhiều sinh viên trong chu kỳ.
    /// Hỗ trợ cả thao tác trên Grid (nhiều sinh viên) và Form (1 sinh viên).
    /// </summary>
    [HttpPut("cycles/{cycleId:guid}/internships/{internshipId:guid}/evaluations")]
    [ProducesResponseType(typeof(List<SaveEvaluationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveEvaluations(
        [FromRoute] Guid cycleId,
        [FromRoute] Guid internshipId,
        [FromBody] SaveEvaluationsCommand command,
        CancellationToken cancellationToken)
    {
        var evaluatorIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(evaluatorIdStr, out var evaluatorId))
            return Unauthorized();

        var result = await _mediator.Send(
            command with { CycleId = cycleId, InternshipId = internshipId, EvaluatorId = evaluatorId },
             cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Cập nhật điểm các tiêu chí cho evaluation. Chỉ được cập nhật khi Status = Draft.
    /// </summary>
    [HttpPut("evaluations/{evaluationId:guid}")]
    [ProducesResponseType(typeof(UpdateEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvaluation(
        [FromRoute] Guid evaluationId,
        [FromBody] UpdateEvaluationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            command with { EvaluationId = evaluationId },
            cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Mentor nộp đánh giá chính thức (Pending/Draft → Submitted) cho TOÀN BỘ học sinh trong 1 nhóm.
    /// Sau khi submit không thể chỉnh sửa nữa.
    /// </summary>
    [HttpPatch("cycles/{cycleId:guid}/internships/{internshipId:guid}/evaluations/submit")]
    [ProducesResponseType(typeof(SubmitEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitEvaluation(
        [FromRoute] Guid cycleId,
        [FromRoute] Guid internshipId,
        [FromBody] SubmitEvaluationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            command with { CycleId = cycleId, InternshipId = internshipId },
            cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Admin/University công bố kết quả đánh giá (Submitted → Published) cho TOÀN BỘ học sinh trong 1 nhóm.
    /// Sinh viên có thể xem điểm sau khi Published.
    /// </summary>
    [HttpPatch("cycles/{cycleId:guid}/internships/{internshipId:guid}/evaluations/publish")]
    [ProducesResponseType(typeof(PublishEvaluationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PublishEvaluation(
        [FromRoute] Guid cycleId,
        [FromRoute] Guid internshipId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new PublishEvaluationCommand { CycleId = cycleId, InternshipId = internshipId },
            cancellationToken);
        return HandleResult(result);
    }
}