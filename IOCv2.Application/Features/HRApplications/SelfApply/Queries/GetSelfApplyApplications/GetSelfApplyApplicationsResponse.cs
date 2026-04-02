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

    // Job info
    public string? JobPostingTitle { get; set; }
    public bool IsJobClosed { get; set; }
    public bool IsJobDeleted { get; set; }

    // Intern Phase info (from job.InternPhase)
    public Guid? InternshipPhaseId { get; set; }
    public string? InternPhaseName { get; set; }
    public DateOnly? InternPhaseStartDate { get; set; }
    public DateOnly? InternPhaseEndDate { get; set; }

    // Audience badge: Public / Targeted
    public JobAudience? Audience { get; set; }
    public string? AudienceLabel { get; set; }

    public DateTime AppliedAt { get; set; }
    public InternshipApplicationStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
}
