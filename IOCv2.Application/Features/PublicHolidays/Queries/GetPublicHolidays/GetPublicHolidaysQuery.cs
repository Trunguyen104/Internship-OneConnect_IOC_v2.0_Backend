using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.PublicHolidays.Queries.GetPublicHolidays;

/// <summary>
/// Query to fetch all public holidays for a given year.
/// </summary>
public record GetPublicHolidaysQuery : IRequest<Result<List<GetPublicHolidaysResponse>>>
{
    /// <summary>
    /// The year to retrieve public holidays for (e.g. 2026).
    /// </summary>
    public int Year { get; init; }
}
