using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Evaluations.Commands.PublishEvaluation;

public class PublishEvaluationResponse
{
    public Guid EvaluationId { get; set; }
    public EvaluationStatus Status { get; set; }

    public decimal? TotalScore { get; set; }
    public DateTime UpdatedAt { get; set; }
}
