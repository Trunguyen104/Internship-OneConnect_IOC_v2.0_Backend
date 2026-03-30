using IOCv2.Application.Features.UniAdminInternship.Common;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentDetail;

public class GetUniAdminStudentDetailResponse
{
    // Header info
    public Guid StudentId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? ClassName { get; set; }
    public string? Major { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public DateOnly? DateOfBirth { get; set; }

    // Internship context
    public Guid ResolvedTermId { get; set; }
    public string TermName { get; set; } = string.Empty;
    public DateOnly TermStartDate { get; set; }
    public DateOnly TermEndDate { get; set; }
    public InternshipUiStatus InternshipStatus { get; set; }

    // Enterprise
    public Guid? EnterpriseId { get; set; }
    public string? EnterpriseName { get; set; }
    public string? EnterprisePosition { get; set; }

    // Mentor
    public Guid? MentorId { get; set; }
    public string? MentorName { get; set; }
    public string? MentorEmail { get; set; }

    // Logbook summary (null if Unplaced/no group)
    public LogbookSummaryDto? Logbook { get; set; }

    // Quick counts
    public int ViolationCount { get; set; }
    public int PublishedEvaluationCount { get; set; }
}
