using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources
{
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

        public async Task<Result<PaginatedResult<GetAllProjectResourcesResponse>>> Handle(
            GetAllProjectResourcesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                

                // Build query
                var query = _unitOfWork.Repository<Domain.Entities.ProjectResources>().Query()
                    .AsNoTracking();

                // Apply search term if provided
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim().ToLower();
                    query = query.Where(x => x.ResourceName != null &&
                        x.ResourceName.ToLower().Contains(term));
                }

                // Apply project filter if provided
                if (request.ProjectId.HasValue)
                {
                    query = query.Where(x => x.ProjectId == request.ProjectId.Value);
                }

                // Apply resource type filter if provided
                if (request.ResourceType.HasValue)
                {
                    query = query.Where(x => x.ResourceType == request.ResourceType.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply sorting
                query = ApplySorting(query, request.SortColumn, request.SortOrder);

                // Apply pagination
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ProjectTo<GetAllProjectResourcesResponse>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                // Create paginated result
                var result = PaginatedResult<GetAllProjectResourcesResponse>.Create(
                    items, totalCount, request.PageNumber, request.PageSize);
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetAllSuccess), items.Count);
           

                return Result<PaginatedResult<GetAllProjectResourcesResponse>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetAllError));
                throw;
            }
        }

        private IQueryable<Domain.Entities.ProjectResources> ApplySorting(
            IQueryable<Domain.Entities.ProjectResources> query,
            string? sortColumn,
            string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";

            return (sortColumn?.ToLower(), isDescending) switch
            {
                ("resourcename", true) => query.OrderByDescending(x => x.ResourceName),
                ("resourcename", false) => query.OrderBy(x => x.ResourceName),

                ("resourcetype", true) => query.OrderByDescending(x => x.ResourceType),
                ("resourcetype", false) => query.OrderBy(x => x.ResourceType),

                ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
                ("createdat", false) => query.OrderBy(x => x.CreatedAt),

                _ => query.OrderByDescending(x => x.CreatedAt)
            };
        }
    }
}
