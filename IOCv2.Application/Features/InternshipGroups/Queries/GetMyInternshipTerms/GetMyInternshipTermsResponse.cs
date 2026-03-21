using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipTerms;

public class GetMyInternshipTermsResponse
{
    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public TermDisplayStatus Status { get; set; }
    public EnrollmentStatus EnrollmentStatus { get; set; }
    public bool IsPlaced { get; set; }
    public Guid? InternshipGroupId { get; set; }
    public string? EnterpriseName { get; set; }
    public string? MentorName { get; set; }
    public string? ProjectName { get; set; }
    public int JourneyStep { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime EnrolledAt { get; set; }
}
