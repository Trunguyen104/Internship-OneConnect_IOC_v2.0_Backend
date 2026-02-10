using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Extensions.Query;
using IOCv2.Application.Features.StakeholderIssues.DTOs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues;

public class GetStakeholderIssuesQueryHandler : IRequestHandler<GetStakeholderIssuesQuery, Result<PagedResult<StakeholderIssueDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetStakeholderIssuesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<PagedResult<StakeholderIssueDto>>> Handle(GetStakeholderIssuesQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<StakeholderIssue>().Query()
            .Include(si => si.Stakeholder)
            .AsNoTracking();

        // Filter by ProjectId
        if (request.ProjectId.HasValue)
        {
            query = query.Where(si => si.Stakeholder.ProjectId == request.ProjectId.Value);
        }

        // Filter by StakeholderId
        if (request.StakeholderId.HasValue)
        {
            query = query.Where(si => si.StakeholderId == request.StakeholderId.Value);
        }

        // Filter by Status
        if (request.Status.HasValue)
        {
            query = query.Where(si => si.Status == request.Status.Value);
        }

        // Search
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

        // Sorting
        var sortMapping = new Dictionary<string, Expression<Func<StakeholderIssue, object?>>>
        {
            { "title", si => si.Title },
            { "status", si => si.Status },
            { "createdat", si => si.CreatedAt },
            { "stakeholdername", si => si.Stakeholder.Name }
        };
        query = query.ApplySorting(request.Pagination.OrderBy, sortMapping, si => si.CreatedAt);

        // Paging
        var result = await query
            .ProjectTo<StakeholderIssueDto>(_mapper.ConfigurationProvider)
            .ToPagedResultAsync(request.Pagination);

        return Result<PagedResult<StakeholderIssueDto>>.Success(result);
    }
}
