using IOCv2.Domain.Enums;

namespace IOCv2.Application.Common.Helpers;

public static class TermStatusHelper
{
    public static TermDisplayStatus GetComputedStatus(DateOnly startDate, DateOnly endDate, TermStatus status)
    {
        if (status == TermStatus.Closed)
            return TermDisplayStatus.Closed;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        if (startDate > today)
            return TermDisplayStatus.Upcoming;

        if (endDate < today)
            return TermDisplayStatus.Ended;

        return TermDisplayStatus.Active;
    }

    public static bool IsUpcoming(DateOnly startDate, DateOnly endDate, TermStatus status)
    {
        return status == TermStatus.Open && startDate > DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static bool IsActive(DateOnly startDate, DateOnly endDate, TermStatus status)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return status == TermStatus.Open && startDate <= today && endDate >= today;
    }

    public static bool IsEnded(DateOnly startDate, DateOnly endDate, TermStatus status)
    {
        return status == TermStatus.Open && endDate < DateOnly.FromDateTime(DateTime.UtcNow);
    }

    public static bool IsClosed(TermStatus status)
    {
        return status == TermStatus.Closed;
    }
}