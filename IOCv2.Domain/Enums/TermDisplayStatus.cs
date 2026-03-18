namespace IOCv2.Domain.Enums;

/// <summary>
/// Computed display status of a term, derived from StartDate, EndDate, and TermStatus.
/// </summary>
public enum TermDisplayStatus
{
    Upcoming = 1,
    Active = 2,
    Ended = 3,
    Closed = 4
}