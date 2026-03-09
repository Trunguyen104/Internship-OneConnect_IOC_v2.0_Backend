namespace IOCv2.Domain.Enums;

/// <summary>
/// Computed display status of a term, derived from StartDate, EndDate, and TermStatus.
/// </summary>
public enum TermDisplayStatus
{
    Upcoming = 0,
    Active = 1,
    Ended = 2,
    Closed = 3
}
