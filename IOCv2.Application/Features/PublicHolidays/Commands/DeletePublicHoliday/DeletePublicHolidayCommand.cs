using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.PublicHolidays.Commands.DeletePublicHoliday;

/// <summary>
/// Command to delete a public holiday by its ID.
/// </summary>
public record DeletePublicHolidayCommand : IRequest<Result<DeletePublicHolidayResponse>>
{
    public Guid PublicHolidayId { get; init; }
}
