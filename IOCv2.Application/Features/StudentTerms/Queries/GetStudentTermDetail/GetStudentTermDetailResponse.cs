using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudentTermDetail;

public class GetStudentTermDetailResponse
{
    public Guid StudentTermId { get; set; }
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Major { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    public Guid TermId { get; set; }
    public string TermName { get; set; } = string.Empty;

    public EnrollmentStatus EnrollmentStatus { get; set; }
    public PlacementStatus PlacementStatus { get; set; }
    public DateOnly EnrollmentDate { get; set; }
    public string? EnrollmentNote { get; set; }

    public Guid? EnterpriseId { get; set; }
    public string? EnterpriseName { get; set; }

    public string? MidtermFeedback { get; set; }
    public string? FinalFeedback { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
