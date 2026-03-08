namespace IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycles;

public class GetEvaluationCyclesResponse
{
    public Guid CycleId { get; set; }
    public Guid TermId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int CriteriaCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
