namespace IOCv2.Application.Features.PublicHolidays.Queries.GetPublicHolidays;

/// <summary>
/// Response DTO for a single public holiday entry.
/// </summary>
public class GetPublicHolidaysResponse
{
    public Guid PublicHolidayId { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
}
