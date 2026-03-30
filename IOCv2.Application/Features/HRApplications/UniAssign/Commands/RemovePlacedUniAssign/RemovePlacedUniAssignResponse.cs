using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Commands.RemovePlacedUniAssign;

public class RemovePlacedUniAssignResponse
{
    public Guid ApplicationId { get; set; }
    public InternshipApplicationStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}
