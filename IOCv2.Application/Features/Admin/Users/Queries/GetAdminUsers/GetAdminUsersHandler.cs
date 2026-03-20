using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Admin.Users.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Admin.Users.Queries.GetAdminUsers
{
    public class GetAdminUsersHandler : IRequestHandler<GetAdminUsersQuery, Result<PaginatedResult<GetAdminUsersResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAdminUsersHandler> _logger;
        private readonly ICacheService _cacheService;

        public GetAdminUsersHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<GetAdminUsersHandler> logger, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<PaginatedResult<GetAdminUsersResponse>>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting paginated Admin Users (Search: {SearchTerm}, Role: {Role}, Status: {Status})", 
                request.SearchTerm, request.Role, request.Status);

            var cacheKey = AdminUserCacheKeys.UserList(
                request.SearchTerm,
                request.Role.HasValue ? (int)request.Role.Value : null,
                request.Status.HasValue ? (int)request.Status.Value : null,
                request.PageNumber,
                request.PageSize,
                request.SortColumn,
                request.SortOrder);

            var cached = await _cacheService.GetAsync<PaginatedResult<GetAdminUsersResponse>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return Result<PaginatedResult<GetAdminUsersResponse>>.Success(cached);
            }

            var query = _unitOfWork.Repository<User>().Query()
                .Include(u => u.UniversityUser).ThenInclude(uu => uu!.University)
                .Include(u => u.EnterpriseUser).ThenInclude(eu => eu!.Enterprise)
                .AsNoTracking();

            // Filter by search term (name or email or code)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(u =>
                    u.FullName.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    u.UserCode.ToLower().Contains(term));
            }

            // Filter by role
            if (request.Role.HasValue)
            {
                query = query.Where(u => u.Role == request.Role.Value);
            }

            // Filter by status
            if (request.Status.HasValue)
            {
                query = query.Where(u => u.Status == request.Status.Value);
            }

            // Sorting
            query = (request.SortColumn?.ToLower(), request.SortOrder?.ToLower()) switch
            {
                ("fullname", "desc")  => query.OrderByDescending(u => u.FullName),
                ("fullname", _)       => query.OrderBy(u => u.FullName),
                ("email", "desc")     => query.OrderByDescending(u => u.Email),
                ("email", _)          => query.OrderBy(u => u.Email),
                ("createdat", "desc") => query.OrderByDescending(u => u.CreatedAt),
                ("createdat", _)      => query.OrderBy(u => u.CreatedAt),
                _                     => query.OrderByDescending(u => u.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var users = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<GetAdminUsersResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved {Count} Admin Users", users.Count);

            var result = PaginatedResult<GetAdminUsersResponse>.Create(users, totalCount, request.PageNumber, request.PageSize);

            await _cacheService.SetAsync(cacheKey, result, AdminUserCacheKeys.Expiration.UserList, cancellationToken);

            return Result<PaginatedResult<GetAdminUsersResponse>>.Success(result);
        }
    }
}
