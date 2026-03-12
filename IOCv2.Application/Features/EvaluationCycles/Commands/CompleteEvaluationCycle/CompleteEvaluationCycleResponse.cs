namespace IOCv2.Application.Features.EvaluationCycles.Commands.CompleteEvaluationCycle;

public class CompleteEvaluationCycleResponse
{
    public Guid CycleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
