namespace IOCv2.Application.Features.PublicHolidays.Commands.DeletePublicHoliday;

public class DeletePublicHolidayResponse
{
    public Guid PublicHolidayId { get; set; }
    public DateOnly Date { get; set; }
}
