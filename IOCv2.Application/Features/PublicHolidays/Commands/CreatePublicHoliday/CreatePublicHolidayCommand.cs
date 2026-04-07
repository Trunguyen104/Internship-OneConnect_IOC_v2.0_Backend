using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.PublicHolidays.Commands.CreatePublicHoliday;

/// <summary>
/// Command to manually add a single public holiday.
/// </summary>
public record CreatePublicHolidayCommand : IRequest<Result<CreatePublicHolidayResponse>>
{
    public DateOnly Date { get; init; }
    public string? Description { get; init; }
}
