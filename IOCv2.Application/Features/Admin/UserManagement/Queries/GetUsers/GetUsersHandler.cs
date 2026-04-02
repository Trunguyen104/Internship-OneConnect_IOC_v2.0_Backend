using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Features.Admin.UserManagement.Common;

namespace IOCv2.Application.Features.Admin.UserManagement.Queries.GetUsers
{
    public class GetUsersHandler : IRequestHandler<GetUsersQuery, Result<PaginatedResult<GetUsersResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetUsersHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUserService;

        public GetUsersHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            ILogger<GetUsersHandler> logger, 
            ICacheService cacheService,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PaginatedResult<GetUsersResponse>>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting paginated Users (Search: {SearchTerm}, Role: {Role}, Status: {Status}) by {AuditorId}", 
                request.SearchTerm, request.Role, request.Status, _currentUserService.UserId);

            var auditorRoleStr = _currentUserService.Role;
            var auditorUnitId = _currentUserService.UnitId;
            
            if (!Enum.TryParse<UserRole>(auditorRoleStr, true, out var auditorRole))
            {
                return Result<PaginatedResult<GetUsersResponse>>.Failure("Invalid auditor role", ResultErrorType.Forbidden);
            }

            var cacheKey = UserManagementCacheKeys.UserList(
                auditorRoleStr,
                auditorUnitId,
                request.SearchTerm,
                (int?)request.Role,
                (int?)request.Status,
                request.PageNumber,
                request.PageSize,
                request.SortColumn,
                request.SortOrder
            );

            var cached = await _cacheService.GetAsync<PaginatedResult<GetUsersResponse>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return Result<PaginatedResult<GetUsersResponse>>.Success(cached);
            }

            var query = _unitOfWork.Repository<User>().Query()
                .Include(u => u.UniversityUser).ThenInclude(uu => uu!.University)
                .Include(u => u.EnterpriseUser).ThenInclude(eu => eu!.Enterprise)
                .AsNoTracking();

            // 1. Hierarchical Scoping
            if (auditorRole == UserRole.SchoolAdmin)
            {
                query = query.Where(u => u.Role == UserRole.Student && u.UniversityUser != null && u.UniversityUser.UniversityId.ToString() == auditorUnitId);
            }
            else if (auditorRole == UserRole.EnterpriseAdmin)
            {
                query = query.Where(u => (u.Role == UserRole.HR || u.Role == UserRole.Mentor) && u.EnterpriseUser != null && u.EnterpriseUser.EnterpriseId.ToString() == auditorUnitId);
            }
            else if (auditorRole != UserRole.SuperAdmin && auditorRole != UserRole.Moderator)
            {
                return Result<PaginatedResult<GetUsersResponse>>.Failure("Access denied", ResultErrorType.Forbidden);
            }

            // 2. Filters
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(term) || u.Email.ToLower().Contains(term) || u.UserCode.ToLower().Contains(term));
            }

            if (request.Role.HasValue)
            {
                query = query.Where(u => u.Role == request.Role.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(u => u.Status == request.Status.Value);
            }

            // 3. Sorting
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
                .ProjectTo<GetUsersResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved {Count} Users", users.Count);

            var result = PaginatedResult<GetUsersResponse>.Create(users, totalCount, request.PageNumber, request.PageSize);

            // Cache with safe key
            await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

            return Result<PaginatedResult<GetUsersResponse>>.Success(result);
        }
    }
}
