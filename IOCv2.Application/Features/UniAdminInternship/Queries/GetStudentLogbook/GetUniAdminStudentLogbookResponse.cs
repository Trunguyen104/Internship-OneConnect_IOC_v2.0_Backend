using IOCv2.Application.Features.UniAdminInternship.Common;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbook;

public class GetUniAdminStudentLogbookResponse
{
    public Guid ResolvedTermId { get; set; }
    public bool HasInternshipGroup { get; set; }
    public LogbookSummaryDto? Summary { get; set; }
    public int TotalWeeks { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<UniAdminLogbookWeekDto> Weeks { get; set; } = new();
}

public class UniAdminLogbookWeekDto
{
    public int WeekNumber { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
    public bool IsCurrentWeek { get; set; }
    public int RequiredCount { get; set; }
    public int SubmittedCount { get; set; }
    public int MissingCount { get; set; }
    public List<UniAdminLogbookDayDto> Days { get; set; } = new();
}

public class UniAdminLogbookDayDto
{
    public Guid? LogbookId { get; set; }
    public DateTime Date { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsRequired { get; set; }
    public bool IsSubmitted { get; set; }
    public bool IsLate { get; set; }
    public bool IsPastDueMissing { get; set; }
    public bool IsPendingMissing { get; set; }
    public LogbookStatus? LogbookStatus { get; set; }
    public string StatusBadge { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public string? Summary { get; set; }
    public string? Issue { get; set; }
    public string? Plan { get; set; }
}

