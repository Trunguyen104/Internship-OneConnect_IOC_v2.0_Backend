using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Evaluations.Commands.PublishEvaluation;

public class PublishEvaluationResponse
{
    public Guid CycleId { get; set; }
    public Guid InternshipId { get; set; }
    public int UpdatedCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
