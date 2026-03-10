using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Extensions.Query;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues
{
    public class GetStakeholderIssuesQueryHandler : IRequestHandler<GetStakeholderIssuesQuery, IOCv2.Application.Common.Models.Result<IOCv2.Application.Common.Models.PaginatedResult<GetStakeholderIssuesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetStakeholderIssuesQueryHandler> _logger;

        public GetStakeholderIssuesQueryHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetStakeholderIssuesQueryHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IOCv2.Application.Common.Models.Result<IOCv2.Application.Common.Models.PaginatedResult<GetStakeholderIssuesResponse>>> Handle(GetStakeholderIssuesQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting paginated StakeholderIssues for Internship: {InternshipId}, Stakeholder: {StakeholderId}", 
                request.InternshipId, request.StakeholderId);

            try
            {
                // Build base query
            var query = _unitOfWork.Repository<StakeholderIssue>()
                .Query()
                .Include(si => si.Stakeholder)
                .AsNoTracking();

            // Apply filters
            if (request.InternshipId.HasValue)
            {
                query = query.Where(si => si.Stakeholder.InternshipId == request.InternshipId.Value);
            }

            if (request.StakeholderId.HasValue)
            {
                query = query.Where(si => si.StakeholderId == request.StakeholderId.Value);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(si => si.Status == request.Status.Value);
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

            // Manual Pagination as per project pattern
            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.Pagination.PageIndex - 1) * request.Pagination.PageSize)
                .Take(request.Pagination.PageSize)
                .ProjectTo<GetStakeholderIssuesResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved {Count} StakeholderIssues", items.Count);

            var paginatedResult = IOCv2.Application.Common.Models.PaginatedResult<GetStakeholderIssuesResponse>.Create(
                items, 
                totalCount, 
                request.Pagination.PageIndex, 
                request.Pagination.PageSize);

            return IOCv2.Application.Common.Models.Result<IOCv2.Application.Common.Models.PaginatedResult<GetStakeholderIssuesResponse>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting StakeholderIssues for Internship: {InternshipId}, Stakeholder: {StakeholderId}", 
                    request.InternshipId, request.StakeholderId);
                return IOCv2.Application.Common.Models.Result<IOCv2.Application.Common.Models.PaginatedResult<GetStakeholderIssuesResponse>>.Failure("An error occurred while getting stakeholder issues", ResultErrorType.Conflict);
            }
        }
    }
}
