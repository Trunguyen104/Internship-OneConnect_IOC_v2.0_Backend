using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Extensions.Query;
using IOCv2.Application.Features.Sprints.Common;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprints;

public class GetSprintsHandler : IRequestHandler<GetSprintsQuery, Result<PaginatedResult<GetSprintsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetSprintsHandler> _logger;

    public GetSprintsHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IMessageService messageService,
        ILogger<GetSprintsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<GetSprintsResponse>>> Handle(
        GetSprintsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting sprints for project {ProjectId} with status filter {StatusFilter}",
            request.ProjectId, request.StatusFilter);

        var cacheKey = SprintCacheKeys.SprintList(
            request.ProjectId,
            request.Pagination.PageIndex,
            request.Pagination.PageSize,
            request.StatusFilter,
            request.Pagination.Search,
            request.Pagination.OrderBy);

        var cachedResult = await _cacheService.GetAsync<PaginatedResult<GetSprintsResponse>>(cacheKey, cancellationToken);
        if (cachedResult is not null)
        {
            _logger.LogInformation("Returning cached sprints for project {ProjectId}", request.ProjectId);
            return Result<PaginatedResult<GetSprintsResponse>>.Success(cachedResult);
        }

        // Build IQueryable — filter at DB level
        var query = _unitOfWork.Repository<Sprint>().Query()
            .AsNoTracking()
            .Where(s => s.ProjectId == request.ProjectId);

        if (request.StatusFilter.HasValue)
        {
            query = query.Where(s => s.Status == request.StatusFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Pagination.Search))
        {
            var search = request.Pagination.Search.ToLower();
            query = query.Where(s =>
                (s.Name != null && s.Name.ToLower().Contains(search)) ||
                (s.Goal != null && s.Goal.ToLower().Contains(search)));
        }

        query = string.IsNullOrWhiteSpace(request.Pagination.OrderBy) ||
                request.Pagination.OrderBy.ToLower().StartsWith("startdate")
            ? query.OrderBy(s => s.StartDate)
            : request.Pagination.OrderBy.ToLower().StartsWith("enddate")
                ? query.OrderBy(s => s.EndDate)
                : request.Pagination.OrderBy.ToLower().StartsWith("name")
                    ? query.OrderBy(s => s.Name)
                    : query.OrderBy(s => s.StartDate);

        var result = await query
            .ProjectTo<GetSprintsResponse>(_mapper.ConfigurationProvider)
            .ToPaginatedResultAsync(request.Pagination.PageIndex, request.Pagination.PageSize, cancellationToken);

        await _cacheService.SetAsync(cacheKey, result, SprintCacheKeys.Expiration.SprintList, cancellationToken);

        _logger.LogInformation("Successfully retrieved {Count} sprints for project {ProjectId}",
            result.Items.Count, request.ProjectId);

        return Result<PaginatedResult<GetSprintsResponse>>.Success(result);
    }
}
