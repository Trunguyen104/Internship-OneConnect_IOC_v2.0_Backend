using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Commands.ApproveUniAssign;

public class ApproveUniAssignResponse
{
    public Guid ApplicationId { get; set; }
    public InternshipApplicationStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public int WithdrawnApplicationsCount { get; set; }
    public DateTime UpdatedAt { get; set; }
}
