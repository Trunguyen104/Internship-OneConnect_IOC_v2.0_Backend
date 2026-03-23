
﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Terms.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Terms.Queries.GetTerms;

public class GetTermsHandler : IRequestHandler<GetTermsQuery, Result<PaginatedResult<GetTermsResponse>>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetTermsHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public GetTermsHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ILogger<GetTermsHandler> logger,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<Result<PaginatedResult<GetTermsResponse>>> Handle(GetTermsQuery request,
        CancellationToken cancellationToken)
    {
    
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin =
                string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            Guid? universityId = null;

            if (isSuperAdmin)
            {
                // SuperAdmin: optionally filter by UniversityId from query param
                universityId = request.UniversityId;
            }
            else
            {
                // SchoolAdmin: resolve university from UniversityUser table
                var universityUser = await _unitOfWork.Repository<UniversityUser>()
                    .Query()
                    .Where(uu => uu.UserId == userId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (universityUser == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.Terms.LogUserNotAssociatedWithUniversity),
                        userId);
                    return Result<PaginatedResult<GetTermsResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.University.NotFound),
                        ResultErrorType.NotFound);
                }

                universityId = universityUser.UniversityId;
            }

            var cacheKey = TermCacheKeys.TermList(universityId, request.SearchTerm, (int?)request.Status, request.Year, request.PageNumber, request.PageSize, request.SortColumn, request.SortOrder);
            var cached = await _cacheService.GetAsync<PaginatedResult<GetTermsResponse>>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<PaginatedResult<GetTermsResponse>>.Success(cached);

            // Build query
            var query = _unitOfWork.Repository<Term>()
                .Query()
                .AsNoTracking();

            // SuperAdmin without UniversityId filter → all universities; otherwise filter by university
            if (universityId.HasValue) query = query.Where(t => t.UniversityId == universityId.Value);

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(searchTerm));
            }

            // Apply status filter
            if (request.Status.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.UtcNow);

                query = request.Status.Value switch
                {
                    TermDisplayStatus.Upcoming => query.Where(t => t.Status == TermStatus.Open && t.StartDate > today),
                    TermDisplayStatus.Active => query.Where(t =>
                        t.Status == TermStatus.Open && t.StartDate <= today && t.EndDate >= today),
                    TermDisplayStatus.Ended => query.Where(t => t.Status == TermStatus.Open && t.EndDate < today),
                    TermDisplayStatus.Closed => query.Where(t => t.Status == TermStatus.Closed),
                    _ => query
                };
            }

            // Apply year filter
            if (request.Year.HasValue) query = query.Where(t => t.StartDate.Year == request.Year.Value);

            // Get total count
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = ApplySorting(query, request.SortColumn, request.SortOrder);

            // Apply pagination
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<GetTermsResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var result =
                PaginatedResult<GetTermsResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Terms.LogTermsRetrieved), items.Count,
                universityId);

            await _cacheService.SetAsync(cacheKey, result, TermCacheKeys.Expiration.TermList, cancellationToken);

            return Result<PaginatedResult<GetTermsResponse>>.Success(result);
    
    }

    private IQueryable<Term> ApplySorting(IQueryable<Term> query, string? sortColumn, string? sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return (sortColumn?.ToLower(), isDescending) switch
        {
            ("name", true) => query.OrderByDescending(t => t.Name),
            ("name", false) => query.OrderBy(t => t.Name),
            ("startdate", true) => query.OrderByDescending(t => t.StartDate),
            ("startdate", false) => query.OrderBy(t => t.StartDate),
            ("createdat", true) => query.OrderByDescending(t => t.CreatedAt),
            ("createdat", false) => query.OrderBy(t => t.CreatedAt),
            _ => query.OrderByDescending(t => t.CreatedAt)
        };
    }
}
