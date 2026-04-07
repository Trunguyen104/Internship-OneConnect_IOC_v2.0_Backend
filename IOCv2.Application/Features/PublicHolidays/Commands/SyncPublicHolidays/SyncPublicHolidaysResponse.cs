namespace IOCv2.Application.Features.PublicHolidays.Commands.SyncPublicHolidays;

public class SyncPublicHolidaysResponse
{
    /// <summary>
    /// Year that was synchronized.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Country code that was synchronized.
    /// </summary>
    public string CountryCode { get; set; } = string.Empty;

    /// <summary>
    /// Number of NEW holidays inserted into the database.
    /// </summary>
    public int SyncedCount { get; set; }

    /// <summary>
    /// Number of holidays skipped because they already existed.
    /// </summary>
    public int SkippedCount { get; set; }
}
