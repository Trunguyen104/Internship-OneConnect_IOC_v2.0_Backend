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
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogGetAll), request.SearchTerm, request.VisibilityStatus);

            var isMentor = string.Equals(_currentUserService?.Role, "Mentor", StringComparison.OrdinalIgnoreCase);
            var isStudent = string.Equals(_currentUserService?.Role, "Student", StringComparison.OrdinalIgnoreCase);

            var effectiveStudentId = request.StudentId;
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

            // B2: Thêm userId vào cache key để tránh data leak giữa các Mentor/Student
            string? scopedUserId = (isMentor || isStudent) ? _currentUserService?.UserId : null;

            var cacheKey = ProjectCacheKeys.ProjectList(
                request.SearchTerm,
                request.VisibilityStatus.HasValue ? (int)request.VisibilityStatus.Value : null,
                request.OperationalStatus.HasValue ? (int)request.OperationalStatus.Value : null,
                request.ShowArchived,
                request.FromDate,
                request.ToDate,
                request.InternshipId,
                effectiveStudentId,
                request.PageNumber,
                request.PageSize,
                request.SortColumn,
                request.SortOrder,
                request.Field,
                scopedUserId);

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

            if (request.VisibilityStatus.HasValue)
                query = query.Where(p => p.VisibilityStatus == request.VisibilityStatus.Value);

            if (request.OperationalStatus.HasValue)
                query = query.Where(p => p.OperationalStatus == request.OperationalStatus.Value);

            // Hide Archived by default
            if (!request.ShowArchived)
                query = query.Where(p => p.OperationalStatus != OperationalStatus.Archived);

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

            // F3: Filter theo Field (AC-01)
            if (!string.IsNullOrWhiteSpace(request.Field))
            {
                query = query.Where(p => p.Field == request.Field);
            }

            if (effectiveStudentId.HasValue)
            {
                var sid = effectiveStudentId.Value;
                var studentGroupIds = _unitOfWork.Repository<InternshipStudent>().Query()
                    .Where(s => s.StudentId == sid)
                    .Select(s => (Guid?)s.InternshipId);
                query = query.Where(p => studentGroupIds.Contains(p.InternshipId));
            }

            // 3. Apply role-based visibility filters
            if (isMentor)
            {
                // B1: Mentor chỉ thấy project do chính mình tạo (Draft + Published)
                if (!Guid.TryParse(_currentUserService?.UserId, out var mentorUserId))
                    return Result<PaginatedResult<GetAllProjectsResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

                var mentorEnterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == mentorUserId, cancellationToken);

                if (mentorEnterpriseUser != null)
                {
                    query = query.Where(ProjectOwnershipPolicy.BuildMentorVisibility(mentorEnterpriseUser.EnterpriseUserId));
                }
                else
                {
                    // Mentor không có enterprise user record — không có project nào
                    query = query.Where(p => false);
                }
            }
            else if (isStudent)
            {
                // Student: only Published + Active or Completed
                query = query.Where(p =>
                    p.VisibilityStatus == VisibilityStatus.Published &&
                    (p.OperationalStatus == OperationalStatus.Active || p.OperationalStatus == OperationalStatus.Completed));
            }
            else
            {
                // HR, UniAdmin, Admin, EnterpriseAdmin, SchoolAdmin: only Published
                // SuperAdmin has no role filter — sees everything
                var isSuperAdmin = string.Equals(_currentUserService?.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
                if (!isSuperAdmin)
                {
                    query = query.Where(p => p.VisibilityStatus == VisibilityStatus.Published);
                }
            }

            // 4. Get Total Count
            var totalCount = await query.CountAsync(cancellationToken);

            // 5. Sorting
            query = ApplySorting(query, request.SortColumn, request.SortOrder);

            // 6. Pagination & Mapping
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
                ("visibilitystatus", true) => query.OrderByDescending(p => p.VisibilityStatus),
                ("visibilitystatus", false) => query.OrderBy(p => p.VisibilityStatus),
                ("operationalstatus", true) => query.OrderByDescending(p => p.OperationalStatus),
                ("operationalstatus", false) => query.OrderBy(p => p.OperationalStatus),
                ("createdat", true) => query.OrderByDescending(p => p.CreatedAt),
                ("createdat", false) => query.OrderBy(p => p.CreatedAt),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };
        }
    }
}
