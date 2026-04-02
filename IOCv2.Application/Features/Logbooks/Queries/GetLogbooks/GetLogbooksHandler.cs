using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Logbooks.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbooks
{
    public record GetLogbooksHandler : IRequestHandler<GetLogbooksQuery, Result<GetLogbooksByWeekResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetLogbooksHandler> _logger;
        private readonly ICacheService _cacheService;

        public GetLogbooksHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService, ILogger<GetLogbooksHandler> logger, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<GetLogbooksByWeekResponse>> Handle(GetLogbooksQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching weekly logbooks for internship {InternshipId}", request.InternshipId);
            var selectedWeeks = ParseWeekFilter(request.WeekFilter);
            var normalizedWeekFilter = selectedWeeks.Count == 0 ? null : string.Join(",", selectedWeeks);

            var internship = await _unitOfWork.Repository<InternshipGroup>().GetByIdAsync(request.InternshipId, cancellationToken);

            if (internship == null)
            {
                _logger.LogWarning("Internship not found: {InternshipId}", request.InternshipId);
                return Result<GetLogbooksByWeekResponse>.Failure("Internship group not found", ResultErrorType.NotFound);
            }

            var cacheKey = LogbookCacheKeys.LogbookList(
                request.InternshipId,
                1,
                int.MaxValue,
                (int?)request.Status,
                normalizedWeekFilter,
                request.SortColumn,
                request.SortOrder);

            var cached = await _cacheService.GetAsync<GetLogbooksByWeekResponse>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<GetLogbooksByWeekResponse>.Success(cached);

            var query = _unitOfWork.Repository<Logbook>()
                        .Query()
                        .AsNoTracking()
                        .Include(x => x.Student!)
                            .ThenInclude(s => s.User!)
                        .Include(x => x.Internship)
                        .Where(x => x.InternshipId == request.InternshipId);

            // Filter by status
            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }

            // Sorting
            query = (request.SortColumn?.ToLower(), request.SortOrder?.ToLower()) switch
            {
                ("studentname", "desc") => query.OrderByDescending(x => x.Student!.User!.FullName),
                ("studentname", _) => query.OrderBy(x => x.Student!.User!.FullName),
                ("createdat", "desc") => query.OrderByDescending(x => x.CreatedAt),
                ("createdat", _) => query.OrderBy(x => x.CreatedAt),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var logbooks = await query
                .ProjectTo<GetLogbooksResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var internshipStart = (internship.StartDate ?? logbooks.MinBy(x => x.DateReport)?.DateReport ?? DateTime.UtcNow).Date;
            var internshipAnchor = GetStartOfWeek(internshipStart);

            var withWeekNumber = logbooks
                .Select(x => new
                {
                    Item = x,
                    WeekNumber = GetWeekNumber(x.DateReport, internshipAnchor)
                });

            if (selectedWeeks.Count > 0)
            {
                withWeekNumber = withWeekNumber.Where(x => selectedWeeks.Contains(x.WeekNumber));
            }

            var weekGroups = withWeekNumber
                .GroupBy(x => x.WeekNumber)
                .OrderBy(x => x.Key)
                .Select(g =>
                {
                    var weekStart = internshipAnchor.AddDays((g.Key - 1) * 7);
                    var weekEnd = weekStart.AddDays(4);
                    var weekItems = g
                        .Select(x => x.Item)
                        .OrderBy(x => x.DateReport)
                        .ToList();

                    var submitted = weekItems.Count(x => IsSubmittedStatus(x.Status));
                    var total = weekItems.Count;
                    var completionState = submitted == total ? "COMPLETED" : "INCOMPLETE";

                    return new LogbookWeekGroupResponse
                    {
                        WeekNumber = g.Key,
                        WeekLabel = $"Week {g.Key}",
                        WeekStartDate = weekStart,
                        WeekEndDate = weekEnd,
                        WeekRangeText = $"{weekStart:dd/MM/yyyy} - {weekEnd:dd/MM/yyyy}",
                        SubmittedCount = submitted,
                        TotalCount = total,
                        CompletionText = $"{submitted}/{total} {completionState}",
                        Items = weekItems
                    };
                })
                .ToList();

            var responseSubmitted = weekGroups.Sum(x => x.SubmittedCount);
            var responseTotal = weekGroups.Sum(x => x.TotalCount);
            DateTime? rangeStart = weekGroups.Count > 0 ? weekGroups.Min(x => x.WeekStartDate) : null;
            DateTime? rangeEnd = weekGroups.Count > 0 ? weekGroups.Max(x => x.WeekEndDate) : null;

            var weeklyResult = new GetLogbooksByWeekResponse
            {
                InternshipId = request.InternshipId,
                RangeStartDate = rangeStart,
                RangeEndDate = rangeEnd,
                SelectedWeeks = selectedWeeks.OrderBy(x => x).ToList(),
                SubmittedCount = responseSubmitted,
                TotalCount = responseTotal,
                Overview = $"{responseSubmitted}/{responseTotal} days submitted",
                Weeks = weekGroups
            };

            _logger.LogInformation("Retrieved {WeekCount} week groups for internship {InternshipId}", weeklyResult.Weeks.Count, request.InternshipId);

            await _cacheService.SetAsync(cacheKey, weeklyResult, LogbookCacheKeys.Expiration.LogbookList, cancellationToken);

            return Result<GetLogbooksByWeekResponse>.Success(weeklyResult);
        }

        private static DateTime GetStartOfWeek(DateTime date)
        {
            var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-diff);
        }

        private static int GetWeekNumber(DateTime date, DateTime internshipAnchor)
        {
            var diffDays = (date.Date - internshipAnchor).Days;
            return (diffDays / 7) + 1;
        }

        private static HashSet<int> ParseWeekFilter(string? weekFilter)
        {
            if (string.IsNullOrWhiteSpace(weekFilter))
            {
                return new HashSet<int>();
            }

            return weekFilter
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => int.TryParse(x, out var n) && n > 0)
                .Select(int.Parse)
                .ToHashSet();
        }

        private static bool IsSubmittedStatus(LogbookStatus status)
        {
            return status is LogbookStatus.SUBMITTED
                or LogbookStatus.APPROVED
                or LogbookStatus.PUNCTUAL
                or LogbookStatus.LATE;
        }

    }
}
