using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipPhases.Commands.UpdateInternshipPhase;

public class UpdateInternshipPhaseResponse
{
    public Guid PhaseId { get; set; }
    public Guid EnterpriseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int? MaxStudents { get; set; }
    public string? Description { get; set; }
    public InternshipPhaseStatus Status { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
