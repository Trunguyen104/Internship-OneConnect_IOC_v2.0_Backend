using IOCv2.Application.Features.EvaluationCycles.Commands.CreateEvaluationCycle;
using IOCv2.Application.Features.EvaluationCycles.Commands.DeleteEvaluationCycle;
using IOCv2.Application.Features.EvaluationCycles.Commands.UpdateEvaluationCycle;
using IOCv2.Application.Features.EvaluationCycles.Commands.StartEvaluationCycle;
using IOCv2.Application.Features.EvaluationCycles.Commands.CompleteEvaluationCycle;
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
using IOCv2.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IOCv2.API.Controllers;

/// <summary>
/// Evaluation Management — manage evaluation cycles, criteria, and mentor grading.
/// </summary>
[Tags("Evaluations")]
[Authorize]
public class EvaluationsController : ApiControllerBase
{
    private readonly IMediator _mediator;

    public EvaluationsController(IMediator mediator) => _mediator = mediator;

    // ==========================================
    // EVALUATION CYCLES
    // ==========================================

    /// <summary>
    /// Get evaluation cycles filtered by term.
    /// </summary>
    [HttpGet("cycles")]
    [ProducesResponseType(typeof(ApiResponse<List<GetEvaluationCyclesResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvaluationCycles([FromQuery] Guid termId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEvaluationCyclesQuery { TermId = termId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Get details of an evaluation cycle including its criteria.
    /// </summary>
    [HttpGet("cycles/{cycleId:guid}", Name = "GetEvaluationCycleById")]
    [ProducesResponseType(typeof(ApiResponse<GetEvaluationCycleByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvaluationCycleById(Guid cycleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEvaluationCycleByIdQuery(cycleId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Create a new evaluation cycle.
    /// </summary>
    [HttpPost("cycles")]
    [ProducesResponseType(typeof(ApiResponse<CreateEvaluationCycleResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEvaluationCycle(
        [FromBody] CreateEvaluationCycleCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return HandleCreateResult(result, nameof(GetEvaluationCycleById), new { cycleId = result.Data?.CycleId, version = "1" });
    }

    /// <summary>
    /// Update evaluation cycle information.
    /// </summary>
    [HttpPut("cycles/{cycleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateEvaluationCycleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvaluationCycle(
        Guid cycleId,
        [FromBody] UpdateEvaluationCycleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { CycleId = cycleId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft delete an evaluation cycle. Cannot delete if criteria exist.
    /// </summary>
    [HttpDelete("cycles/{cycleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvaluationCycle(Guid cycleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteEvaluationCycleCommand(cycleId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Bắt đầu chu kỳ đánh giá (Pending -> Grading).
    /// </summary>
    [HttpPatch("cycles/{cycleId:guid}/start")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartEvaluationCycle(Guid cycleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new StartEvaluationCycleCommand { CycleId = cycleId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Hoàn thành chu kỳ đánh giá (Grading -> Completed).
    /// </summary>
    [HttpPatch("cycles/{cycleId:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteEvaluationCycle(Guid cycleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new CompleteEvaluationCycleCommand { CycleId = cycleId }, cancellationToken);
        return HandleResult(result);
    }

    // ==========================================
    // EVALUATION CRITERIA
    // ==========================================

    /// <summary>
    /// Get criteria for a specific evaluation cycle.
    /// </summary>
    [HttpGet("criteria")]
    [ProducesResponseType(typeof(ApiResponse<List<GetEvaluationCriteriaResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvaluationCriteria([FromQuery] Guid cycleId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetEvaluationCriteriaQuery(cycleId), cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Add new criteria to an evaluation cycle.
    /// </summary>
    [HttpPost("cycles/{cycleId:guid}/criteria")]
    [ProducesResponseType(typeof(ApiResponse<CreateEvaluationCriteriaResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateEvaluationCriteria(
        [FromRoute] Guid cycleId,
        [FromBody] CreateEvaluationCriteriaCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { CycleId = cycleId }, cancellationToken);
        return HandleCreateResult(result, nameof(GetEvaluationCriteria), new { cycleId = cycleId, version = "1" });
    }

    /// <summary>
    /// Update evaluation criteria.
    /// </summary>
    [HttpPut("criteria/{criteriaId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateEvaluationCriteriaResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateEvaluationCriteria(
        Guid criteriaId,
        [FromBody] UpdateEvaluationCriteriaCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { CriteriaId = criteriaId }, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Soft delete evaluation criteria.
    /// </summary>
    [HttpDelete("criteria/{criteriaId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEvaluationCriteria(Guid criteriaId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new DeleteEvaluationCriteriaCommand(criteriaId), cancellationToken);
        return HandleResult(result);
    }

    // ==========================================
    // EVALUATIONS
    // ==========================================

    /// <summary>
    /// Get all evaluations in a cycle for an internship group (Grid View).
    /// </summary>
    [HttpGet("cycles/{cycleId:guid}/internships/{internshipId:guid}/evaluations")]
    [ProducesResponseType(typeof(ApiResponse<GetInternshipEvaluationsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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
    /// Save (Upsert) evaluations for one or more students.
    /// </summary>
    [HttpPut("cycles/{cycleId:guid}/internships/{internshipId:guid}/evaluations")]
    [ProducesResponseType(typeof(ApiResponse<List<SaveEvaluationsResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SaveEvaluations(
        [FromRoute] Guid cycleId,
        [FromRoute] Guid internshipId,
        [FromBody] SaveEvaluationsCommand command,
        CancellationToken cancellationToken)
    {
        var evaluatorId = GetCurrentUserId();
        if (evaluatorId == null)
            return Unauthorized(ApiResponse<object>.Fail("Unauthorized access."));

        var result = await _mediator.Send(
            command with { CycleId = cycleId, InternshipId = internshipId, EvaluatorId = evaluatorId.Value },
             cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Update scores for a specific evaluation. Only allowed when Status = Draft.
    /// </summary>
    [HttpPut("evaluations/{evaluationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UpdateEvaluationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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
    /// Mentor officially submits evaluations (Draft -> Submitted) for all students in a group.
    /// Cannot edit after submission.
    /// </summary>
    [HttpPatch("cycles/{cycleId:guid}/internships/{internshipId:guid}/evaluations/submit")]
    [ProducesResponseType(typeof(ApiResponse<SubmitEvaluationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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
    /// University publishes results (Submitted -> Published). Students can view scores after publication.
    /// </summary>
    [HttpPatch("cycles/{cycleId:guid}/internships/{internshipId:guid}/evaluations/publish")]
    [ProducesResponseType(typeof(ApiResponse<PublishEvaluationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
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
