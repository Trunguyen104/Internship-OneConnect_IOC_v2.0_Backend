using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudentTermDetail;

public record GetStudentTermDetailResponse
{
    // Student term identity
    public Guid StudentTermId { get; init; }
    public Guid TermId { get; init; }
    public Guid StudentId { get; init; }

    // Student profile (editable)
    public string StudentCode { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Phone { get; init; }
    public DateOnly? DateOfBirth { get; init; }
    public string? Major { get; init; }
    public string? AvatarUrl { get; init; }

    // Enrollment info
    public DateOnly EnrollmentDate { get; init; }
    public EnrollmentStatus EnrollmentStatus { get; init; }
    public string? EnrollmentNote { get; init; }

    // Placement info
    public PlacementStatus PlacementStatus { get; init; }
    public Guid? EnterpriseId { get; init; }
    public string? EnterpriseName { get; init; }

    // Feedback (read-only)
    public string? MidtermFeedback { get; init; }
    public string? FinalFeedback { get; init; }

    // Audit
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}
