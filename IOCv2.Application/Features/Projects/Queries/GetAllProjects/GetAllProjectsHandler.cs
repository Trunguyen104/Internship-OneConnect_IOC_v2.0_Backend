using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetAllProjects
{
    public class GetAllProjectsHandler : IRequestHandler<GetAllProjectsQuery, Result<PaginatedResult<GetAllProjectsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllProjectsHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService? _currentUserService;

        public GetAllProjectsHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetAllProjectsHandler> logger,
            IMessageService messageService,
            ICacheService cacheService,
            ICurrentUserService? currentUserService = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PaginatedResult<GetAllProjectsResponse>>> Handle(
            GetAllProjectsQuery request,
            CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogGetAll), request.SearchTerm, request.Status);

            var effectiveStudentId = request.StudentId;
            var isStudent = string.Equals(_currentUserService?.Role, "Student", StringComparison.OrdinalIgnoreCase);
            if (isStudent)
            {
                if (!Guid.TryParse(_currentUserService?.UserId, out var currentUserId))
                    return Result<PaginatedResult<GetAllProjectsResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

                var currentStudentId = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .Where(s => s.Student.UserId == currentUserId)
                    .Select(s => s.StudentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (currentStudentId == Guid.Empty)
                    return Result<PaginatedResult<GetAllProjectsResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                if (request.StudentId.HasValue && request.StudentId.Value != currentStudentId)
                    return Result<PaginatedResult<GetAllProjectsResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                effectiveStudentId = currentStudentId;
            }

            var cacheKey = ProjectCacheKeys.ProjectList(
                request.SearchTerm,
                request.Status.HasValue ? (int)request.Status.Value : null,
                request.FromDate,
                request.ToDate,
                request.InternshipId,
                effectiveStudentId,
                request.PageNumber,
                request.PageSize,
                request.SortColumn,
                request.SortOrder);

            var cached = await _cacheService.GetAsync<PaginatedResult<GetAllProjectsResponse>>(cacheKey, cancellationToken);
            if (cached != null)
            {
                return Result<PaginatedResult<GetAllProjectsResponse>>.Success(cached);
            }

            // 1. Build base query
            var query = _unitOfWork.Repository<Project>().Query().AsNoTracking();

            // 2. Apply Filters (FFA-FLW)
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    p.ProjectName.ToLower().Contains(term) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            if (request.Status.HasValue)
            {
                query = query.Where(p => p.Status == request.Status.Value);
            }


            if (request.FromDate.HasValue)
            {
                query = query.Where(p => p.StartDate >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(p => p.EndDate <= request.ToDate.Value);
            }

            if (request.InternshipId.HasValue)
            {
                query = query.Where(p => p.InternshipId == request.InternshipId.Value);
            }

            if (effectiveStudentId.HasValue)
            {
                var sid = effectiveStudentId.Value;
                var studentGroupIds = _unitOfWork.Repository<InternshipStudent>().Query()
                    .Where(s => s.StudentId == sid)
                    .Select(s => (Guid?)s.InternshipId);
                query = query.Where(p => studentGroupIds.Contains(p.InternshipId));
            }

            if (isStudent)
            {
                query = query.Where(p => p.Status == ProjectStatus.Published || p.Status == ProjectStatus.Completed);
            }

            // 3. Get Total Count
            var totalCount = await query.CountAsync(cancellationToken);

            // 4. Sorting
            query = ApplySorting(query, request.SortColumn, request.SortOrder);

            // 5. Pagination & Mapping
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<GetAllProjectsResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var result = PaginatedResult<GetAllProjectsResponse>.Create(
                items, totalCount, request.PageNumber, request.PageSize);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogGetAllSuccess), items.Count);

            await _cacheService.SetAsync(cacheKey, result, ProjectCacheKeys.Expiration.ProjectList, cancellationToken);

            return Result<PaginatedResult<GetAllProjectsResponse>>.Success(result);
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
