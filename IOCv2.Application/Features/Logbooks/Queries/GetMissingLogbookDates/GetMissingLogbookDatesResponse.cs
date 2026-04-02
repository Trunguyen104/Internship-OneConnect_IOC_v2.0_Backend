namespace IOCv2.Application.Features.Logbooks.Queries.GetMissingLogbookDates;

/// <summary>
/// Response containing the list of working days on which a student missed logbook submission.
/// </summary>
public class GetMissingLogbookDatesResponse
{
    /// <summary>
    /// The ID of the student these missing dates belong to.
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// The start date of the student's active internship phase (used as calculation window start).
    /// </summary>
    public DateOnly InternshipStartDate { get; set; }

    /// <summary>
    /// End of the calculation window (today's UTC date).
    /// </summary>
    public DateOnly CalculatedUpTo { get; set; }

    /// <summary>
    /// Total number of working days in the window (excluding weekends and holidays).
    /// </summary>
    public int TotalWorkingDays { get; set; }

    /// <summary>
    /// Number of days the student has submitted a logbook.
    /// </summary>
    public int SubmittedDays { get; set; }

    /// <summary>
    /// The list of dates on which the student did NOT submit a logbook entry
    /// (weekends and public holidays already excluded).
    /// </summary>
    public List<DateOnly> MissingDates { get; set; } = new();
}
