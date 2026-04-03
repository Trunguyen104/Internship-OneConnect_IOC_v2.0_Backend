namespace IOCv2.Application.Interfaces;

/// <summary>
/// Abstraction for fetching public holidays from an external API.
/// Implemented in Infrastructure to satisfy Dependency Inversion.
/// </summary>
public interface IPublicHolidayApiService
{
    /// <summary>
    /// Fetches public holidays for the specified country and year from the external API.
    /// </summary>
    /// <param name="year">The year to fetch holidays for.</param>
    /// <param name="countryCode">ISO 3166-1 alpha-2 country code (e.g. "VN").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of holidays with their dates and names.</returns>
    Task<IReadOnlyList<ExternalHolidayDto>> GetHolidaysAsync(
        int year,
        string countryCode,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// DTO representing one holiday returned by the external API.
/// Lives in Application layer to keep Infrastructure isolated.
/// </summary>
public record ExternalHolidayDto(DateOnly Date, string Name);
