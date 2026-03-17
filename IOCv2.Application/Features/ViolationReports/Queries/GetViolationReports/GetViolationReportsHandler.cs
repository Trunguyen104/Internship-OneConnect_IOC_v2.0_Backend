using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ViolationReport;
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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace IOCv2.Application.Features.ViolationReports.Queries.GetViolationReports
{
    /// <summary>
    /// Handler for GetViolationReportsQuery.
    /// Builds and executes an EF query with search, filters, role-based access,
    /// sorting and pagination, then projects to GetViolationReportsResponse.
    /// </summary>
    public class GetViolationReportsHandler : IRequestHandler<GetViolationReportsQuery, Result<PaginatedResult<GetViolationReportsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly ILogger<GetViolationReportsHandler> _logger;
        private readonly IMessageService _messageService;

        public GetViolationReportsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IMapper mapper, ILogger<GetViolationReportsHandler> logger, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
        }

        /// <summary>
        /// Handle GetViolationReportsQuery:
        /// - Compose base query with required includes (student, user, internship group, mentor.user)
        /// - Apply role-based restriction for Mentors
        /// - Apply search and filter criteria (createdBy, occurred date range, group)
        /// - Count total results (before pagination)
        /// - Return an empty paginated result when no items (so frontend can render empty state)
        /// - Apply sorting (CreatedAt DESC by default, toggleable via request.OrderByCreatedAscending)
        /// - Apply pagination and projection to DTO
        /// - Return success or mapped failure on exception
        /// </summary>
        public async Task<Result<PaginatedResult<GetViolationReportsResponse>>> Handle(GetViolationReportsQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Base query: include student -> user and internshipGroup -> mentor -> user for mapping & filtering.
                var query = _unitOfWork.Repository<ViolationReport>().Query()
                    .Include(x => x.Student).ThenInclude(s => s.User)
                    .Include(x => x.InternshipGroup).ThenInclude(g => g.Mentor).ThenInclude(m => m.User)
                    .AsNoTracking();

                // Role-based access:
                // If the current user is a Mentor, limit reports to groups that the mentor manages.
                if (ViolationReportParam.MentorRole.Equals(_currentUserService.Role))
                {
                    var currentUserId = Guid.Parse(_currentUserService.UserId!);
                    // Ensure InternshipGroup.Mentor exists and matches current user id
                    query = query.Where(x => x.InternshipGroup.Mentor != null && x.InternshipGroup.Mentor.UserId == currentUserId);
                }

                // Search: match student full name or student code (case-insensitive)
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim().ToLower();
                    query = query.Where(x => x.Student.User.FullName.ToLower().Contains(term) || x.Student.User.UserCode.ToLower().Contains(term));
                }

                // Filters: CreatedBy, Occurred date range, GroupId
                if (request.CreatedById.HasValue) query = query.Where(x => x.CreatedBy == request.CreatedById.Value);

                if (request.OccurredFrom.HasValue) query = query.Where(x => x.OccurredDate >= request.OccurredFrom.Value);

                if (request.OccurredTo.HasValue) query = query.Where(x => x.OccurredDate <= request.OccurredTo.Value);

                if (request.GroupId.HasValue) query = query.Where(x => x.InternshipGroupId == request.GroupId.Value);

                // Total count BEFORE pagination so frontend can show total number ("Tổng cộng X báo cáo").
                var totalCount = await query.CountAsync(cancellationToken);

                // If no results, return an empty paginated result (frontend will render empty state).
                // Returning NotFound here would prevent rendering an empty list with pagination/summary.
                if (totalCount == 0)
                {
                    var emptyResponse = PaginatedResult<GetViolationReportsResponse>.Create(
                        new List<GetViolationReportsResponse>(),
                        0,
                        request.PageNumber,
                        request.PageSize
                    );
                    return Result<PaginatedResult<GetViolationReportsResponse>>.Success(emptyResponse);
                }

                // Sorting: default newest first (CreatedAt DESC).
                // Client may toggle via request.OrderByCreatedAscending.
                var orderedQuery = request.OrderByCreatedAscending
                    ? query.OrderBy(x => x.CreatedAt)
                    : query.OrderByDescending(x => x.CreatedAt);

                // Pagination + Projection: apply Skip/Take, then ProjectTo DTO using AutoMapper configuration.
                var items = await orderedQuery
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ProjectTo<GetViolationReportsResponse>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                var response = PaginatedResult<GetViolationReportsResponse>.Create(
                    items,
                    totalCount,
                    request.PageNumber,
                    request.PageSize
                );

                return Result<PaginatedResult<GetViolationReportsResponse>>.Success(response);
            }
            catch (Exception ex)
            {
                // Log and return a generic internal server error key so client can display a localized message.
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ViolationReportKey.GetViolationReportsError));
                return Result<PaginatedResult<GetViolationReportsResponse>>.Failure(MessageKeys.ViolationReportKey.GetViolationReportsError, ResultErrorType.InternalServerError);
            }
        }
    }
}
