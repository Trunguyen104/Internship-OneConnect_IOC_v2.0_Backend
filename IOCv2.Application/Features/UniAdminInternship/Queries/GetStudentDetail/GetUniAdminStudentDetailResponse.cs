using IOCv2.Application.Features.UniAdminInternship.Common;
using IOCv2.Domain.Enums;

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
    public List<UniAdminWeeklyLogbookDto> LogbookWeeks { get; set; } = new();

    // Quick counts
    public int ViolationCount { get; set; }
    public int PublishedEvaluationCount { get; set; }
}

public class UniAdminWeeklyLogbookDto
{
    public int WeekNumber { get; set; }
    public string WeekTitle { get; set; } = string.Empty;
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public int SubmittedCount { get; set; }
    public int LateCount { get; set; }
    public int TotalCount { get; set; }
    public string CompletionRatio { get; set; } = string.Empty;
    public List<UniAdminWeeklyLogbookEntryDto> Entries { get; set; } = new();
}

public class UniAdminWeeklyLogbookEntryDto
{
    public Guid LogbookId { get; set; }
    public DateTime DateReport { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Issue { get; set; }
    public string Plan { get; set; } = string.Empty;
    public LogbookStatus Status { get; set; }
    public string StatusBadge { get; set; } = string.Empty;
}

