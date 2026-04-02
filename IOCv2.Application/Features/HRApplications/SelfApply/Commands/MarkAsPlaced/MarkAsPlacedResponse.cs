using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Commands.MarkAsPlaced;

public class MarkAsPlacedResponse
{
    public Guid ApplicationId { get; set; }
    public InternshipApplicationStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
}
