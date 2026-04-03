using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Stakeholders.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders
{
    public class GetStakeholdersHandler : IRequestHandler<GetStakeholdersQuery, Result<PaginatedResult<GetStakeholdersResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetStakeholdersHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public GetStakeholdersHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetStakeholdersHandler> logger,
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

        public async Task<Result<PaginatedResult<GetStakeholdersResponse>>> Handle(GetStakeholdersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting paginated stakeholders for internship {InternshipId}", request.InternshipId);


            // Check internship exists
            var internshipExists = await _unitOfWork.Repository<InternshipGroup>()
                .ExistsAsync(p => p.InternshipId == request.InternshipId, cancellationToken);

            if (!internshipExists)
            {
                _logger.LogWarning("InternshipGroup {InternshipId} not found", request.InternshipId);
                return Result<PaginatedResult<GetStakeholdersResponse>>.NotFound("InternshipGroup not found");
            }

            var authError = StakeholderAccessGuard.EnsureAuthenticated<PaginatedResult<GetStakeholdersResponse>>(_currentUserService, _messageService);
            if (authError is not null)
            {
                return authError;
            }

            var accessError = await StakeholderAccessGuard.EnsureInternshipAccessAsync<PaginatedResult<GetStakeholdersResponse>>(
                _unitOfWork,
                _messageService,
                _currentUserService,
                request.InternshipId,
                cancellationToken);

            if (accessError is not null)
            {
                _logger.LogWarning("User {UserId} attempted to get stakeholders in internship {InternshipId} without permission", _currentUserService.UserId, request.InternshipId);
                return accessError;
            }

            var cacheKey = StakeholderCacheKeys.StakeholderList(
                request.InternshipId,
                request.SearchTerm,
                request.SortColumn,
                request.SortOrder,
                request.PageNumber,
                request.PageSize);

            var cached = await _cacheService.GetAsync<PaginatedResult<GetStakeholdersResponse>>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<PaginatedResult<GetStakeholdersResponse>>.Success(cached);

            // Build base query
            var query = _unitOfWork.Repository<Stakeholder>()
                .Query()
                .Where(s => s.InternshipId == request.InternshipId)
                .AsNoTracking();

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    (s.Role != null && s.Role.ToLower().Contains(term)) ||
                    s.Email.ToLower().Contains(term));
            }

            // Sorting
            query = (request.SortColumn?.ToLower(), request.SortOrder?.ToLower()) switch
            {
                ("name", "desc") => query.OrderByDescending(s => s.Name),
                ("name", _) => query.OrderBy(s => s.Name),
                ("email", "desc") => query.OrderByDescending(s => s.Email),
                ("email", _) => query.OrderBy(s => s.Email),
                ("createdat", "desc") => query.OrderByDescending(s => s.CreatedAt),
                ("createdat", _) => query.OrderBy(s => s.CreatedAt),
                _ => query.OrderBy(s => s.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<GetStakeholdersResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved {Count} stakeholders for internship {InternshipId}", items.Count, request.InternshipId);

            var result = PaginatedResult<GetStakeholdersResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);
            await _cacheService.SetAsync(cacheKey, result, StakeholderCacheKeys.Expiration.StakeholderList, cancellationToken);
            return Result<PaginatedResult<GetStakeholdersResponse>>.Success(result);

        }
    }
}

