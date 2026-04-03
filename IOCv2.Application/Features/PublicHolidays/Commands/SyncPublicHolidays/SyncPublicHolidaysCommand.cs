using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.PublicHolidays.Commands.SyncPublicHolidays;

/// <summary>
/// Command to synchronize public holidays for a given year from the external Calendarific API.
/// New holidays are inserted; existing dates are skipped to avoid duplicates.
/// </summary>
public record SyncPublicHolidaysCommand : IRequest<Result<SyncPublicHolidaysResponse>>
{
    /// <summary>
    /// The year to sync (e.g. 2026).
    /// </summary>
    public int Year { get; init; }

    /// <summary>
    /// ISO 3166-1 alpha-2 country code. Defaults to "VN".
    /// </summary>
    public string CountryCode { get; init; } = "VN";
}
