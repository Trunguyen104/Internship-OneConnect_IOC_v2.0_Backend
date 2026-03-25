using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycles;

public class GetEvaluationCyclesResponse
{
    public Guid CycleId { get; set; }
    public Guid PhaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public EvaluationCycleStatus Status { get; set; }

    public int CriteriaCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
