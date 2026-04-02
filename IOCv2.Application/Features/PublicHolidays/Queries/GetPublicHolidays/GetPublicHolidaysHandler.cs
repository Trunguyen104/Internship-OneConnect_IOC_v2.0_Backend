using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.PublicHolidays.Queries.GetPublicHolidays;

public class GetPublicHolidaysHandler
    : IRequestHandler<GetPublicHolidaysQuery, Result<List<GetPublicHolidaysResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetPublicHolidaysHandler> _logger;

    public GetPublicHolidaysHandler(IUnitOfWork unitOfWork, ILogger<GetPublicHolidaysHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<List<GetPublicHolidaysResponse>>> Handle(
        GetPublicHolidaysQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching public holidays for year {Year}.", request.Year);

        var holidays = await _unitOfWork.Repository<PublicHoliday>()
            .Query()
            .AsNoTracking()
            .Where(h => h.Date.Year == request.Year)
            .OrderBy(h => h.Date)
            .Select(h => new GetPublicHolidaysResponse
            {
                PublicHolidayId = h.PublicHolidayId,
                Date            = h.Date,
                Description     = h.Description
            })
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} public holidays for year {Year}.", holidays.Count, request.Year);

        return Result<List<GetPublicHolidaysResponse>>.Success(holidays);
    }
}
