using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetAProjects
{
    public class GetAllProjectsHandler : IRequestHandler<GetAllProjectsQuery, Result<PaginatedResult<GetAllProjectsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllProjectsHandler> _logger;
        private readonly IMessageService _messageService;

        public GetAllProjectsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetAllProjectsHandler> logger,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<PaginatedResult<GetAllProjectsResponse>>> Handle(
            GetAllProjectsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                // 1. Build base query
                var query = _unitOfWork.Repository<Project>().Query().Select(x => x).AsNoTracking();

                // 2. Apply search term
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim().ToLower();
                    query = query.Where(p =>
                        p.ProjectName.ToLower().Contains(term) ||
                        (p.Description != null && p.Description.ToLower().Contains(term)));
                }

                // 3. Apply status filter
                if (request.Status.HasValue)
                {
                    query = query.Where(p => p.Status == request.Status.Value);
                }

                // 4. Apply date range filter
                if (request.FromDate.HasValue)
                {
                    query = query.Where(p => p.StartDate >= request.FromDate.Value);
                }

                if (request.ToDate.HasValue)
                {
                    query = query.Where(p => p.EndDate <= request.ToDate.Value);
                }

                // 5. Get total count before pagination
                var totalCount = await query.CountAsync(cancellationToken);

                // 6. Apply sorting
                query = ApplySorting(query, request.SortColumn, request.SortOrder);

                // 7. Apply pagination
                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ProjectTo<GetAllProjectsResponse>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                // 8. Create paginated result
                var result = PaginatedResult<GetAllProjectsResponse>.Create(
                    items, totalCount, request.PageNumber, request.PageSize);

                return Result<PaginatedResult<GetAllProjectsResponse>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Projects.GetAllError));
                throw;
            }
        }

        private IQueryable<Project> ApplySorting(IQueryable<Project> query, string? sortColumn, string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";

            return (sortColumn?.ToLower(), isDescending) switch
            {
                ("projectname", true) => query.OrderByDescending(p => p.ProjectName),
                ("projectname", false) => query.OrderBy(p => p.ProjectName),

                ("startdate", true) => query.OrderByDescending(p => p.StartDate),
                ("startdate", false) => query.OrderBy(p => p.StartDate),

                ("enddate", true) => query.OrderByDescending(p => p.EndDate),
                ("enddate", false) => query.OrderBy(p => p.EndDate),

                ("status", true) => query.OrderByDescending(p => p.Status),
                ("status", false) => query.OrderBy(p => p.Status),

                ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
                ("createdat", false) => query.OrderBy(p => p.CreatedAt),

                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }
    }
}
