namespace IOCv2.Application.Features.Evaluations.Commands.UpdateEvaluation;

public class UpdateEvaluationResponse
{
    public Guid EvaluationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public string? Note { get; set; }
    public int DetailCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}
