using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.UniAdminInternship.Common;

public class UniAdminWeeklyLogbookDto
{
    public int WeekNumber { get; set; }
    public string WeekTitle { get; set; } = string.Empty;
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public int SubmittedCount { get; set; }
    public int OnTimeCount { get; set; }
    public int LateCount { get; set; }
    public int MissingCount { get; set; }
    public int PendingCount { get; set; }
    public int TotalCount { get; set; }
    public string CompletionRatio { get; set; } = string.Empty;
    public bool IsCurrentWeek { get; set; }
    public List<UniAdminWeeklyLogbookEntryDto> Entries { get; set; } = new();
}

public class UniAdminWeeklyLogbookEntryDto
{
    public Guid LogbookId { get; set; }
    public DateTime DateReport { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Issue { get; set; }
    public string Plan { get; set; } = string.Empty;
    public LogbookStatus? Status { get; set; }
    public string StatusBadge { get; set; } = string.Empty;
    public DateTime? SubmittedAt { get; set; }
    public bool IsSubmitted { get; set; }
    public bool IsLate { get; set; }
    public bool IsMissing { get; set; }
    public bool IsFuture { get; set; }
    public bool IsWeekend { get; set; }
    public bool IsHoliday { get; set; }
    public bool IsRequired { get; set; }
    public List<UniAdminLogbookWorkItemDto> WorkItems { get; set; } = new();
}

public class UniAdminLogbookWorkItemDto
{
    public Guid WorkItemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? StoryPoint { get; set; }
    public DateOnly? DueDate { get; set; }
}

