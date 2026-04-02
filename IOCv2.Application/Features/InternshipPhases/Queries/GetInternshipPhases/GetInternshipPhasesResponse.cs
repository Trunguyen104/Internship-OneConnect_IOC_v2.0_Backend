using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhases;

public class GetInternshipPhasesResponse
{
    public Guid PhaseId { get; set; }
    public Guid EnterpriseId { get; set; }
    public string EnterpriseName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string MajorFields { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int RemainingCapacity { get; set; }
    public int JobPostingCount { get; set; }
    public string? Description { get; set; }
    public InternshipPhaseLifecycleStatus Status { get; set; }
    public int GroupCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
