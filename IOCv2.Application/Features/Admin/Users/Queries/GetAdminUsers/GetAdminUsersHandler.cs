using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Admin.Users.Queries.GetAdminUsers
{
    public class GetAdminUsersHandler : IRequestHandler<GetAdminUsersQuery, Result<PaginatedResult<GetAdminUsersResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetAdminUsersHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PaginatedResult<GetAdminUsersResponse>>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken)
        {
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
            if (!string.IsNullOrWhiteSpace(request.Role) && Enum.TryParse<UserRole>(request.Role, true, out var parsedRole))
            {
                query = query.Where(u => u.Role == parsedRole);
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(request.Status) && Enum.TryParse<UserStatus>(request.Status, true, out var parsedStatus))
            {
                query = query.Where(u => u.Status == parsedStatus);
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

            var result = PaginatedResult<GetAdminUsersResponse>.Create(users, totalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<GetAdminUsersResponse>>.Success(result);
        }
    }
}
