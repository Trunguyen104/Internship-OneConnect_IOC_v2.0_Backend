using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.PublicHolidays.Commands.SyncPublicHolidays;

/// <summary>
/// Calls the Calendarific API, compares results with existing DB records,
/// and bulk-inserts only NEW holidays to avoid duplicates.
/// </summary>
public class SyncPublicHolidaysHandler
    : IRequestHandler<SyncPublicHolidaysCommand, Result<SyncPublicHolidaysResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublicHolidayApiService _apiService;
    private readonly IMessageService _messageService;
    private readonly ILogger<SyncPublicHolidaysHandler> _logger;

    public SyncPublicHolidaysHandler(
        IUnitOfWork unitOfWork,
        IPublicHolidayApiService apiService,
        IMessageService messageService,
        ILogger<SyncPublicHolidaysHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _apiService = apiService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<SyncPublicHolidaysResponse>> Handle(
        SyncPublicHolidaysCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Syncing public holidays for {Country}/{Year} from Calendarific.",
            request.CountryCode, request.Year);

        // ── 1. Fetch from external API ────────────────────────────────────────
        IReadOnlyList<ExternalHolidayDto> fetched;
        try
        {
            fetched = await _apiService.GetHolidaysAsync(
                request.Year, request.CountryCode, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Calendarific API call failed for {Country}/{Year}.",
                request.CountryCode, request.Year);
            return Result<SyncPublicHolidaysResponse>.Failure(
                _messageService.GetMessage(MessageKeys.PublicHolidays.SyncApiError),
                ResultErrorType.InternalServerError);
        }

        if (fetched.Count == 0)
        {
            _logger.LogWarning(
                "Calendarific returned 0 holidays for {Country}/{Year}.",
                request.CountryCode, request.Year);

            return Result<SyncPublicHolidaysResponse>.Success(new SyncPublicHolidaysResponse
            {
                Year         = request.Year,
                CountryCode  = request.CountryCode,
                SyncedCount  = 0,
                SkippedCount = 0
            });
        }

        // ── 2. Load existing dates for this year (avoid N+1) ─────────────────
        var existingDates = await _unitOfWork.Repository<PublicHoliday>()
            .Query()
            .AsNoTracking()
            .Where(h => h.Date.Year == request.Year)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        var existingSet = new HashSet<DateOnly>(existingDates);

        // ── 3. Filter to only NEW holidays ───────────────────────────────────
        var newHolidays = fetched
            .Where(dto => !existingSet.Contains(dto.Date))
            .Select(dto => PublicHoliday.Create(dto.Date, dto.Name))
            .ToList();

        int skippedCount = fetched.Count - newHolidays.Count;

        // ── 4. Bulk insert ────────────────────────────────────────────────────
        if (newHolidays.Count > 0)
        {
            foreach (var h in newHolidays)
                await _unitOfWork.Repository<PublicHoliday>().AddAsync(h, cancellationToken);

            await _unitOfWork.SaveChangeAsync(cancellationToken);
        }

        _logger.LogInformation(
            "Sync complete for {Country}/{Year}: {Synced} inserted, {Skipped} skipped.",
            request.CountryCode, request.Year, newHolidays.Count, skippedCount);

        return Result<SyncPublicHolidaysResponse>.Success(
            new SyncPublicHolidaysResponse
            {
                Year         = request.Year,
                CountryCode  = request.CountryCode,
                SyncedCount  = newHolidays.Count,
                SkippedCount = skippedCount
            },
            _messageService.GetMessage(MessageKeys.PublicHolidays.SyncSuccess));
    }
}
