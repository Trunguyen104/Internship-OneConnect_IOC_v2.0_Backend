using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.UniAdminInternship.Common;

internal static class UniAdminLogbookCalculator
{
    public static LogbookSummaryDto? CalculateLogbookSummary(
        InternshipStudent? internStudent,
        InternshipGroup? group,
        int submittedCount,
        int lateCount = 0)
    {
        if (internStudent == null || group == null)
            return null;

        var joinedAt = internStudent.JoinedAt.Date;
        var phaseEnd = group.EndDate?.Date ?? DateTime.UtcNow.Date;
        var effectiveEnd = DateTime.UtcNow.Date < phaseEnd ? DateTime.UtcNow.Date : phaseEnd;

        if (joinedAt > effectiveEnd)
            return new LogbookSummaryDto
            {
                Missing = 0,
                Submitted = submittedCount,
                Late = lateCount,
                OnTime = Math.Max(0, submittedCount - lateCount),
                Total = 0,
                PercentComplete = 0
            };

        var total = CountBusinessDays(joinedAt, effectiveEnd);
        var missing = Math.Max(0, total - submittedCount);
        var percent = total > 0 ? (int)Math.Round((double)submittedCount / total * 100) : 0;

        return new LogbookSummaryDto
        {
            Missing = missing,
            Submitted = submittedCount,
            Late = lateCount,
            OnTime = Math.Max(0, submittedCount - lateCount),
            Total = total,
            PercentComplete = percent
        };
    }

    public static List<UniAdminWeeklyLogbookDto> BuildWeeklyLogbooks(
        List<Logbook> logbooks,
        DateTime joinedAt,
        DateTime? phaseEndDate,
        IMessageService messageService)
    {
        var startDate = joinedAt.Date;
        var endDate = phaseEndDate?.Date ?? DateTime.UtcNow.Date;
        if (endDate < startDate)
            return new List<UniAdminWeeklyLogbookDto>();

        var today = DateTime.UtcNow.Date;
        var logbookByDate = logbooks
            .GroupBy(x => x.DateReport.Date)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.CreatedAt).First());

        var weeks = new List<UniAdminWeeklyLogbookDto>();
        var weekStart = GetStartOfWeek(startDate);
        var weekNumber = 1;

        while (weekStart.Date <= endDate)
        {
            var weekEnd = weekStart.AddDays(6);
            var rangeStart = weekStart.Date < startDate ? startDate : weekStart.Date;
            var rangeEnd = weekEnd.Date > endDate ? endDate : weekEnd.Date;

            var entries = new List<UniAdminWeeklyLogbookEntryDto>();
            for (var d = rangeStart; d <= rangeEnd; d = d.AddDays(1))
            {
                var isWeekend = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
                var isHoliday = false;
                var isRequired = !isWeekend && !isHoliday;

                if (logbookByDate.TryGetValue(d, out var logbook))
                {
                    var isLate = logbook.Status == LogbookStatus.LATE;
                    entries.Add(new UniAdminWeeklyLogbookEntryDto
                    {
                        LogbookId = logbook.LogbookId,
                        DateReport = d,
                        Summary = logbook.Summary,
                        Issue = logbook.Issue,
                        Plan = logbook.Plan,
                        Status = logbook.Status,
                        StatusBadge = isLate
                            ? messageService.GetMessage(MessageKeys.UniAdminInternship.StatusBadgeLate)
                            : messageService.GetMessage(MessageKeys.UniAdminInternship.StatusBadgeSubmitted),
                        SubmittedAt = logbook.CreatedAt,
                        IsSubmitted = true,
                        IsLate = isLate,
                        IsMissing = false,
                        IsFuture = false,
                        IsWeekend = isWeekend,
                        IsHoliday = isHoliday,
                        IsRequired = isRequired,
                        WorkItems = logbook.WorkItems
                            .Where(w => w.DeletedAt == null)
                            .Select(w => new UniAdminLogbookWorkItemDto
                            {
                                WorkItemId = w.WorkItemId,
                                Title = w.Title,
                                Description = w.Description,
                                Type = w.Type.ToString(),
                                Status = w.Status?.ToString(),
                                Priority = w.Priority?.ToString(),
                                StoryPoint = w.StoryPoint,
                                DueDate = w.DueDate
                            }).ToList()
                    });
                }
                else
                {
                    var isFuture = d >= today;
                    var statusBadge = isWeekend
                        ? messageService.GetMessage(MessageKeys.UniAdminInternship.StatusBadgeWeekend)
                        : isHoliday
                            ? messageService.GetMessage(MessageKeys.UniAdminInternship.StatusBadgeHoliday)
                            : isFuture
                                ? messageService.GetMessage(MessageKeys.UniAdminInternship.StatusBadgePending)
                                : messageService.GetMessage(MessageKeys.UniAdminInternship.StatusBadgeMissing);

                    entries.Add(new UniAdminWeeklyLogbookEntryDto
                    {
                        LogbookId = Guid.Empty,
                        DateReport = d,
                        Summary = string.Empty,
                        Issue = null,
                        Plan = string.Empty,
                        Status = null,
                        StatusBadge = statusBadge,
                        SubmittedAt = null,
                        IsSubmitted = false,
                        IsLate = false,
                        IsMissing = !isFuture && isRequired,
                        IsFuture = isFuture && isRequired,
                        IsWeekend = isWeekend,
                        IsHoliday = isHoliday,
                        IsRequired = isRequired
                    });
                }
            }

            var submittedCount = entries.Count(x => x.IsSubmitted);
            var onTimeCount = entries.Count(x => x.IsSubmitted && !x.IsLate);
            var lateCount = entries.Count(x => x.IsLate);
            var missingCount = entries.Count(x => x.IsMissing);
            var pendingCount = entries.Count(x => x.IsFuture && x.IsRequired);
            var requiredCount = entries.Count(x => x.IsRequired);

            weeks.Add(new UniAdminWeeklyLogbookDto
            {
                WeekNumber = weekNumber,
                WeekTitle = messageService.GetMessage(
                    MessageKeys.UniAdminInternship.WeekTitle,
                    weekNumber,
                    rangeStart.ToString("dd/MM"),
                    rangeEnd.ToString("dd/MM/yyyy")),
                WeekStartDate = rangeStart,
                WeekEndDate = rangeEnd,
                SubmittedCount = submittedCount,
                OnTimeCount = onTimeCount,
                LateCount = lateCount,
                MissingCount = missingCount,
                PendingCount = pendingCount,
                TotalCount = requiredCount,
                CompletionRatio = requiredCount == 0 ? "0/0" : $"{submittedCount}/{requiredCount}",
                IsCurrentWeek = today >= rangeStart && today <= rangeEnd,
                Entries = entries
            });

            weekStart = weekStart.AddDays(7);
            weekNumber++;
        }

        return weeks;
    }

    private static int CountBusinessDays(DateTime start, DateTime end)
    {
        var count = 0;
        for (var d = start.Date; d <= end.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }

        return count;
    }

    private static DateTime GetStartOfWeek(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }
}

