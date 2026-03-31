using IOCv2.Application.Features.InternshipPhases.Common;

namespace IOCv2.Application.Features.InternshipPhases.Commands.UpdateInternshipPhase;

public class UpdateInternshipPhaseResponse
{
    public Guid PhaseId { get; set; }
    public Guid EnterpriseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string MajorFields { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int RemainingCapacity { get; set; }
    public string? Description { get; set; }
    public InternshipPhaseLifecycleStatus Status { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
