using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources
{
    /// <summary>
    /// Handler to fetch paginated list of ProjectResources with optional filtering, sorting and search.
    /// </summary>
    public class GetAllProjectResourcesHandler : IRequestHandler<GetAllProjectResourcesQuery, Result<PaginatedResult<GetAllProjectResourcesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllProjectResourcesHandler> _logger;
        private readonly IMessageService _messageService;
        public GetAllProjectResourcesHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetAllProjectResourcesHandler> logger,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
        }

        /// <summary>
        /// Build and execute a query that supports:
        /// - optional search by resource name
        /// - optional project and resource type filters
        /// - sorting by provided column/order
        /// - pagination (page number / size)
        /// </summary>
        public async Task<Result<PaginatedResult<GetAllProjectResourcesResponse>>> Handle(
            GetAllProjectResourcesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // Start with repository queryable and avoid tracking for read-only operations.
                var query = _unitOfWork.Repository<Domain.Entities.ProjectResources>().Query()
                    .AsNoTracking();

                // Apply search term (case-insensitive contains).
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim().ToLower();
                    query = query.Where(x => x.ResourceName != null &&
                        x.ResourceName.ToLower().Contains(term));
                }

                // Filter by project if provided.
                if (request.ProjectId.HasValue)
                {
                    query = query.Where(x => x.ProjectId == request.ProjectId.Value);
                }

                // Filter by resource type if provided.
                if (request.ResourceType.HasValue)
                {
                    query = query.Where(x => x.ResourceType == request.ResourceType.Value);
                }

                // Get total count before pagination.
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply ordering.
                query = ApplySorting(query, request.SortColumn, request.SortOrder);

                // Apply pagination and projection to DTO.
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ProjectTo<GetAllProjectResourcesResponse>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                // Construct paginated result.
                var result = PaginatedResult<GetAllProjectResourcesResponse>.Create(
                    items, totalCount, request.PageNumber, request.PageSize);

                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetAllSuccess), items.Count);

                return Result<PaginatedResult<GetAllProjectResourcesResponse>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetAllError));
                return Result<PaginatedResult<GetAllProjectResourcesResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }

        /// <summary>
        /// Applies sorting to the query based on column and order.
        /// Defaults to CreatedAt descending when unknown.
        /// </summary>
        private IQueryable<Domain.Entities.ProjectResources> ApplySorting(
            IQueryable<Domain.Entities.ProjectResources> query,
            string? sortColumn,
            string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == ProjectResourceParams.Filter.Desc;

            return (sortColumn?.ToLower(), isDescending) switch
            {
                (ProjectResourceParams.Filter.ResourceName, true) => query.OrderByDescending(x => x.ResourceName),
                (ProjectResourceParams.Filter.ResourceName, false) => query.OrderBy(x => x.ResourceName),

                (ProjectResourceParams.Filter.ResourceType, true) => query.OrderByDescending(x => x.ResourceType),
                (ProjectResourceParams.Filter.ResourceType, false) => query.OrderBy(x => x.ResourceType),

                (ProjectResourceParams.Filter.CreateDate, true) => query.OrderByDescending(x => x.CreatedAt),
                (ProjectResourceParams.Filter.CreateDate, false) => query.OrderBy(x => x.CreatedAt),

                _ => query.OrderByDescending(x => x.CreatedAt)
            };
        }
    }
}
