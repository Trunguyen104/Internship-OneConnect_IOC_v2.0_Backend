using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetMyInternshipPhases;

public class GetMyInternshipPhasesResponse
{
    public Guid PhaseId { get; set; }
    public string PhaseName { get; set; } = string.Empty;
    public InternshipPhaseStatus PhaseStatus { get; set; }
    public Guid InternshipGroupId { get; set; }
    public string? EnterpriseName { get; set; }
    public string? MentorName { get; set; }
    public string? ProjectName { get; set; }
    public int JourneyStep { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
}
