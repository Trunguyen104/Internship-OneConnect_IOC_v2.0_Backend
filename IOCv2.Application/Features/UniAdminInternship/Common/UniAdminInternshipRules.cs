using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.UniAdminInternship.Common;

public static class UniAdminInternshipRules
{
    public static bool IsPendingApplicationStatus(InternshipApplicationStatus status)
    {
        return status is InternshipApplicationStatus.Applied
            or InternshipApplicationStatus.Interviewing
            or InternshipApplicationStatus.Offered
            or InternshipApplicationStatus.PendingAssignment;
    }

    public static bool IsSubmittedLogbookStatus(LogbookStatus status)
    {
        return status is LogbookStatus.SUBMITTED
            or LogbookStatus.APPROVED
            or LogbookStatus.PUNCTUAL
            or LogbookStatus.LATE;
    }

    public static InternshipUiStatus DeriveUiStatus(
        PlacementStatus placementStatus,
        InternshipGroup? group,
        bool hasPendingApplication)
    {
        if (placementStatus == PlacementStatus.Unplaced)
            return hasPendingApplication ? InternshipUiStatus.PendingConfirmation : InternshipUiStatus.Unplaced;

        if (group == null)
            return InternshipUiStatus.NoGroup;

        var today = DateTime.UtcNow.Date;
        var isEndedByDate = group.EndDate.HasValue && group.EndDate.Value.Date < today;
        var isFinished = group.Status == GroupStatus.Finished;

        return isEndedByDate || isFinished
            ? InternshipUiStatus.Completed
            : InternshipUiStatus.Active;
    }

    public static LogbookSummaryDto? CalculateLogbookSummary(
        InternshipStudent? internStudent,
        InternshipGroup? group,
        IEnumerable<DateTime>? submittedDates)
    {
        if (internStudent == null || group == null)
            return null;

        var today = DateTime.UtcNow.Date;
        var joinedAt = internStudent.JoinedAt.Date;
        var phaseEnd = group.EndDate?.Date ?? today;
        var effectiveEnd = today < phaseEnd ? today : phaseEnd;

        if (joinedAt > effectiveEnd)
        {
            return new LogbookSummaryDto
            {
                Missing = 0,
                Submitted = 0,
                Total = 0,
                PercentComplete = 0
            };
        }

        var total = CountBusinessDays(joinedAt, effectiveEnd);
        var submitted = (submittedDates ?? Enumerable.Empty<DateTime>())
            .Select(x => x.Date)
            .Where(x => x >= joinedAt && x <= effectiveEnd)
            .Where(x => x.DayOfWeek != DayOfWeek.Saturday && x.DayOfWeek != DayOfWeek.Sunday)
            .Distinct()
            .Count();

        var missing = Math.Max(0, total - submitted);
        var percent = total > 0 ? (int)Math.Round((double)submitted / total * 100) : 0;

        return new LogbookSummaryDto
        {
            Missing = missing,
            Submitted = submitted,
            Total = total,
            PercentComplete = percent
        };
    }

    public static int CountBusinessDays(DateTime start, DateTime end)
    {
        var count = 0;
        for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
        {
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                count++;
        }

        return count;
    }
}

