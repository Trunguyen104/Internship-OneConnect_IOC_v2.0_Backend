using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StudentApplications.Queries.GetMyApplicationDetail;

public class GetMyApplicationDetailResponse
{
    public Guid ApplicationId { get; set; }
    public ApplicationSource Source { get; set; }
    public string SourceLabel => Source == ApplicationSource.SelfApply ? "Self-apply" : "Uni Assign";

    // Application details
    public string? JobTitle { get; set; }
    public string? JobAudience { get; set; }

    // Phase info
    public Guid? InternshipPhaseId { get; set; }
    public string? InternPhaseName { get; set; }
    public DateOnly? InternPhaseStartDate { get; set; }
    public DateOnly? InternPhaseEndDate { get; set; }

    public string EnterpriseName { get; set; } = string.Empty;
    public string? EnterpriseLogoUrl { get; set; }

    public string? CvSnapshotUrl { get; set; }

    public InternshipApplicationStatus Status { get; set; }
    public string? RejectReason { get; set; }
    public DateTime AppliedAt { get; set; }

    // Flags
    public bool CanWithdraw { get; set; }
    public bool CanHide { get; set; }

    public List<ApplicationHistoryDto> History { get; set; } = new();
}

public class ApplicationHistoryDto
{
    public InternshipApplicationStatus Status { get; set; }
    public string StatusLabel => Status.ToString();
    public DateTime ChangedAt { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public string? Note { get; set; }
}
