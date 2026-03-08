using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups
{
    public class GetInternshipGroupsHandler : IRequestHandler<GetInternshipGroupsQuery, Result<PaginatedResult<GetInternshipGroupsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetInternshipGroupsHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PaginatedResult<GetInternshipGroupsResponse>>> Handle(GetInternshipGroupsQuery request, CancellationToken cancellationToken)
        {
            var query = _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(ig => ig.Enterprise)
                .Include(ig => ig.Mentor!).ThenInclude(m => m.User!)
                .Include(ig => ig.Members)
                .AsNoTracking();

            // Lọc theo GroupName hoặc EnterpriseName nếu có SearchTerm
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var lowerSearch = request.SearchTerm.ToLower();
                query = query.Where(x => x.GroupName.ToLower().Contains(lowerSearch) ||
                                        (x.Enterprise != null && x.Enterprise.Name.ToLower().Contains(lowerSearch)));
            }

            // Lọc theo Status
            if (request.Status.HasValue)
            {
                query = query.Where(x => x.Status == request.Status.Value);
            }

            // Sắp xếp mặc định theo ngày bắt đầu giảm dần hoặc tên
            query = query.OrderByDescending(x => x.CreatedAt);

            // Xử lý Pagination
            var totalCount = await query.CountAsync(cancellationToken);

            var entities = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var resultItems = _mapper.Map<List<GetInternshipGroupsResponse>>(entities);

            var paginatedResult = new PaginatedResult<GetInternshipGroupsResponse>(resultItems, totalCount, request.PageNumber, request.PageSize);

            return Result<PaginatedResult<GetInternshipGroupsResponse>>.Success(paginatedResult);
        }
    }
}
