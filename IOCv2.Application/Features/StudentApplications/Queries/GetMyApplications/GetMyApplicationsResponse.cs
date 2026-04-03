using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StudentApplications.Queries.GetMyApplications;

public class GetMyApplicationsResponse
{
    public Guid ApplicationId { get; set; }
    public ApplicationSource Source { get; set; }
    public string SourceLabel => Source == ApplicationSource.SelfApply ? "Self-apply" : "Uni Assign";

    // Job info (Self-apply)
    public string? JobTitle { get; set; }
    public bool? IsJobClosed { get; set; }
    public bool? IsJobDeleted { get; set; }

    // Phase info
    public Guid? InternshipPhaseId { get; set; }
    public string? InternPhaseName { get; set; }
    public DateOnly? InternPhaseStartDate { get; set; }
    public DateOnly? InternPhaseEndDate { get; set; }

    // Enterprise
    public string EnterpriseName { get; set; } = string.Empty;
    public string? EnterpriseLogoUrl { get; set; }

    public InternshipApplicationStatus Status { get; set; }
    public DateTime AppliedAt { get; set; }

    // Derived action flags (AC-01 table)
    public bool CanWithdraw { get; set; }
    public bool CanHide { get; set; }
}
