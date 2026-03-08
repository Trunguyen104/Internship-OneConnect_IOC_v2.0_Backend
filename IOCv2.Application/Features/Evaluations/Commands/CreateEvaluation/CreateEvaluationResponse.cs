using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Evaluations.Commands.CreateEvaluation;

public class CreateEvaluationResponse
{
    public Guid EvaluationId { get; set; }
    public Guid CycleId { get; set; }
    public string CycleName { get; set; } = string.Empty;
    public Guid InternshipId { get; set; }

    /// <summary>true = đánh giá cả nhóm, false = đánh giá từng cá nhân</summary>
    public bool IsGroupEvaluation { get; set; }

    public Guid? StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid EvaluatorId { get; set; }
    public EvaluationStatus Status { get; set; }

    public decimal? TotalScore { get; set; }
    public string? Note { get; set; }
    public int DetailCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
