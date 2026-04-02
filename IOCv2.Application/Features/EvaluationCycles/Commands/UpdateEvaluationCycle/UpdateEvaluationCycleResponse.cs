using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.UpdateEvaluationCycle;

public class UpdateEvaluationCycleResponse
{
    public Guid CycleId { get; set; }
    public Guid PhaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public EvaluationCycleStatus Status { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
