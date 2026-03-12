namespace IOCv2.Application.Features.EvaluationCycles.Commands.StartEvaluationCycle;

public class StartEvaluationCycleResponse
{
    public Guid CycleId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
