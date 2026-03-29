using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Commands.RejectUniAssign;

public class RejectUniAssignResponse
{
    public Guid ApplicationId { get; set; }
    public InternshipApplicationStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public string RejectReason { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
