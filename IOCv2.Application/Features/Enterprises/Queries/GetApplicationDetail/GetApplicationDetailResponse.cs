using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Enterprises.Queries.GetApplicationDetail;

public class GetApplicationDetailResponse
{
    public Guid ApplicationId { get; set; }
    public Guid EnterpriseId { get; set; }
    public Guid TermId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentFullName { get; set; } = string.Empty;
    public string StudentCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string UniversityName { get; set; } = string.Empty;
    public string Major { get; set; } = string.Empty;
    public InternshipApplicationStatus Status { get; set; }
    public string? RejectReason { get; set; }
    public string? MentorName { get; set; }
    public string? ProjectName { get; set; }
    public DateTime AppliedAt { get; set; }
}
