using IOCv2.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IOCv2.Infrastructure.Services;

/// <summary>
/// Implements IPublicHolidayApiService using the Calendarific REST API
/// (https://calendarific.com/api/v2/holidays).
/// </summary>
public class CalendarificService : IPublicHolidayApiService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CalendarificService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CalendarificService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CalendarificService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ExternalHolidayDto>> GetHolidaysAsync(
        int year,
        string countryCode,
        CancellationToken cancellationToken = default)
    {
        var apiKey  = _configuration["Calendarific:ApiKey"];
        var baseUrl = _configuration["Calendarific:BaseUrl"] ?? "https://calendarific.com/api/v2";

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogError("Calendarific API key is not configured (Calendarific:ApiKey).");
            throw new InvalidOperationException("Calendarific API key is missing. Set CALENDARIFIC_API_KEY environment variable.");
        }

        var url = $"{baseUrl}/holidays?api_key={apiKey}&country={countryCode}&year={year}";

        _logger.LogInformation("Calling Calendarific API: {Url}", url.Replace(apiKey, "***"));

        using var client   = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Calendarific API returned {StatusCode}: {Body}",
                (int)response.StatusCode, body);
            throw new HttpRequestException(
                $"Calendarific API returned {(int)response.StatusCode}: {response.ReasonPhrase}");
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var root = JsonSerializer.Deserialize<CalendarificRoot>(json, JsonOptions);

        if (root?.Response?.Holidays is null)
        {
            _logger.LogWarning("Calendarific returned null or empty holidays list.");
            return Array.Empty<ExternalHolidayDto>();
        }

        var result = root.Response.Holidays
            .Where(h => h.Date?.Iso is not null)
            .Select(h =>
            {
                // iso format: "2026-02-29" or "2026-02-29T00:00:00+07:00"
                var datePart = h.Date!.Iso!.Length >= 10 ? h.Date.Iso[..10] : h.Date.Iso;
                if (!DateOnly.TryParseExact(datePart, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out var date))
                {
                    _logger.LogWarning("Could not parse date '{DateStr}' from Calendarific.", datePart);
                    return null;
                }
                return new ExternalHolidayDto(date, h.Name ?? string.Empty);
            })
            .Where(d => d is not null)
            .Cast<ExternalHolidayDto>()
            // Deduplicate by date (Calendarific sometimes returns the same day twice)
            .DistinctBy(d => d.Date)
            .ToList();

        _logger.LogInformation(
            "Calendarific returned {Count} unique holidays for {Country}/{Year}.",
            result.Count, countryCode, year);

        return result;
    }

    // ─── Private JSON model (Calendarific response shape) ─────────────────────

    private sealed class CalendarificRoot
    {
        [JsonPropertyName("response")]
        public CalendarificResponse? Response { get; set; }
    }

    private sealed class CalendarificResponse
    {
        [JsonPropertyName("holidays")]
        public List<CalendarificHoliday>? Holidays { get; set; }
    }

    private sealed class CalendarificHoliday
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("date")]
        public CalendarificDate? Date { get; set; }
    }

    private sealed class CalendarificDate
    {
        [JsonPropertyName("iso")]
        public string? Iso { get; set; }
    }
}
