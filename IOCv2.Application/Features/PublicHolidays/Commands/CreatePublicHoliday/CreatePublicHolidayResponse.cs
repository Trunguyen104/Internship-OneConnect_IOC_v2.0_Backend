namespace IOCv2.Application.Features.PublicHolidays.Commands.CreatePublicHoliday;

public class CreatePublicHolidayResponse
{
    public Guid PublicHolidayId { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }
}
