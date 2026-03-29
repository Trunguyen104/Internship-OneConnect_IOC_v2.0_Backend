using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Queries.GetUniAssignApplications;

public class GetUniAssignApplicationsResponse
{
    public Guid ApplicationId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string UniversityName { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }
    public InternshipApplicationStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;

    /// <summary>True if SV has an active Self-apply application at this same enterprise — shows ⚠️ icon (AC-10).</summary>
    public bool HasActiveSelfApply { get; set; }

    /// <summary>If HasActiveSelfApply, the current status of the Self-apply application for tooltip.</summary>
    public string? ActiveSelfApplyStatus { get; set; }
}
