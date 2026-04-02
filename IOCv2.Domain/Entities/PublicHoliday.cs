namespace IOCv2.Domain.Entities;

/// <summary>
/// Represents a public holiday that should be excluded from missing logbook date calculations.
/// </summary>
public class PublicHoliday : BaseEntity
{
    public Guid PublicHolidayId { get; set; }

    /// <summary>
    /// The date of the public holiday.
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// Optional description or name of the holiday (e.g., "Tết Nguyên Đán").
    /// </summary>
    public string? Description { get; set; }

    public static PublicHoliday Create(DateOnly date, string? description = null)
    {
        return new PublicHoliday
        {
            PublicHolidayId = Guid.NewGuid(),
            Date = date,
            Description = description
        };
    }
}
