using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Queries.GetSelfApplyApplications;

public class GetSelfApplyApplicationsResponse
{
    public Guid ApplicationId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;
    public string UniversityName { get; set; } = string.Empty;
    public string? JobPostingTitle { get; set; }
    public DateTime AppliedAt { get; set; }
    public InternshipApplicationStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;

    /// <summary>Summary badge counts for all active statuses.</summary>
    public static Dictionary<string, int>? BadgeCounts { get; set; }
}
