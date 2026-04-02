using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.HRApplications.Common.Queries.GetApplicationDetail;

public class GetApplicationDetailResponse
{
    public Guid ApplicationId { get; set; }
    public ApplicationSource Source { get; set; }
    public string SourceLabel { get; set; } = string.Empty;

    // Student info
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string StudentEmail { get; set; } = string.Empty;
    public string StudentPhone { get; set; } = string.Empty;
    public string UniversityName { get; set; } = string.Empty;

    // Job/Assignment info
    public Guid? JobId { get; set; }
    public string? JobPostingTitle { get; set; }
    public bool IsJobClosed { get; set; }
    public bool IsJobDeleted { get; set; }
    public string? CvSnapshotUrl { get; set; }

    // Intern Phase info (from job.InternPhase)
    public Guid? InternshipPhaseId { get; set; }
    public string? InternPhaseName { get; set; }
    public DateOnly? InternPhaseStartDate { get; set; }
    public DateOnly? InternPhaseEndDate { get; set; }

    // Audience
    public JobAudience? Audience { get; set; }
    public string? AudienceLabel { get; set; }

    // Current status
    public InternshipApplicationStatus Status { get; set; }
    public string StatusLabel { get; set; } = string.Empty;
    public DateTime AppliedAt { get; set; }

    // Timeline
    public List<StatusHistoryItem> StatusHistories { get; set; } = new();
}

public class StatusHistoryItem
{
    public InternshipApplicationStatus FromStatus { get; set; }
    public InternshipApplicationStatus ToStatus { get; set; }
    public string FromStatusLabel { get; set; } = string.Empty;
    public string ToStatusLabel { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? ChangedByName { get; set; }
    public string TriggerSource { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
