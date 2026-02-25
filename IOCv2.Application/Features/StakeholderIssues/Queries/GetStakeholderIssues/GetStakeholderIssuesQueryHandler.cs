using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Extensions.Query;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues
{
    public class GetStakeholderIssuesQueryHandler : IRequestHandler<GetStakeholderIssuesQuery, Result<PagedResult<GetStakeholderIssuesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public GetStakeholderIssuesQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        public async Task<Result<PagedResult<GetStakeholderIssuesResponse>>> Handle(GetStakeholderIssuesQuery request, CancellationToken cancellationToken)
        {
            // Build base query with Include
            var query = _unitOfWork.Repository<StakeholderIssue>()
                .Query()
                .Include(si => si.Stakeholder)
                .AsNoTracking();

            // Apply filters
            if (request.ProjectId.HasValue)
            {
                query = query.Where(si => si.Stakeholder.ProjectId == request.ProjectId.Value);
            }

            if (request.StakeholderId.HasValue)
            {
                query = query.Where(si => si.StakeholderId == request.StakeholderId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                if (Enum.TryParse<StakeholderIssueStatus>(request.Status, true, out var statusEnum))
                {
                    query = query.Where(si => si.Status == statusEnum);
                }
            }

            // Apply global search
            if (!string.IsNullOrWhiteSpace(request.Pagination.Search))
            {
                var searchableFields = new List<Expression<Func<StakeholderIssue, string?>>>
                {
                    si => si.Title,
                    si => si.Description,
                    si => si.Stakeholder.Name
                };
                query = query.ApplyGlobalSearch(request.Pagination.Search, searchableFields);
            }

            // Apply sorting
            var sortMapping = new Dictionary<string, Expression<Func<StakeholderIssue, object?>>>
            {
                ["title"] = si => si.Title,
                ["status"] = si => si.Status,
                ["createdat"] = si => si.CreatedAt,
                ["stakeholdername"] = si => si.Stakeholder.Name
            };

            query = query.ApplySorting(
                request.Pagination.OrderBy,
                sortMapping,
                si => si.CreatedAt);

            // Project and paginate
            var pagedResult = await query
                .ProjectTo<GetStakeholderIssuesResponse>(_mapper.ConfigurationProvider)
                .ToPagedResultAsync(request.Pagination);

            return Result<PagedResult<GetStakeholderIssuesResponse>>.Success(pagedResult);
        }
    }
}
