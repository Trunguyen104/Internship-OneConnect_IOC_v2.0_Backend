using IOCv2.Domain.Entities;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.UpdateEvaluationCycle;

public class UpdateEvaluationCycleResponse
{
    public Guid CycleId { get; set; }
    public Guid TermId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? UpdatedAt { get; set; }
}
