using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ViolationReport;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
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
    /// Handles queries to retrieve a paginated list of violation reports.
    /// Responsibilities:
    /// - Build EF query with required Includes for mapping/filtering
    /// - Apply role-based access control (Mentor scoping)
    /// - Apply search and filter criteria
    /// - Compute total count before pagination
    /// - Apply sorting, pagination and project to DTO
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
        /// Compose and execute the query, returning a paginated result of GetViolationReportsResponse DTOs.
        /// Steps:
        /// 1. Build base query with includes required by mappings and filters.
        /// 2. Apply Mentor scoping (if current user is a Mentor).
        /// 3. Apply search term and filters (created by, occurred date range, group).
        /// 4. Calculate total count (before pagination).
        /// 5. Return empty paginated result if no items.
        /// 6. Apply sorting, pagination and projection to DTOs.
        /// 7. Return success or an internal server error on exception.
        /// </summary>
        public async Task<Result<PaginatedResult<GetViolationReportsResponse>>> Handle(GetViolationReportsQuery request, CancellationToken cancellationToken)
        {
            // 1) Base query: include navigation properties used for filtering/mapping.
            var query = _unitOfWork.Repository<ViolationReport>().Query()
                .Include(x => x.Student!).ThenInclude(s => s!.User!)
                .Include(x => x.InternshipGroup!).ThenInclude(g => g!.Mentor!).ThenInclude(m => m!.User!)
                .AsNoTracking();

            // 2) Role-based access.
            var currentRole = _currentUserService.Role ?? string.Empty;
            if (UserRole.Mentor.ToString().Equals(currentRole, StringComparison.OrdinalIgnoreCase))
            {
                var currentUserId = Guid.Parse(_currentUserService.UserId!);
                query = query.Where(x => x.InternshipGroup!.Mentor != null && x.InternshipGroup!.Mentor!.UserId == currentUserId);
            }
            else if (UserRole.EnterpriseAdmin.ToString().Equals(currentRole, StringComparison.OrdinalIgnoreCase))
            {
                if (!Guid.TryParse(_currentUserService.UnitId, out var enterpriseId))
                {
                    _logger.LogWarning("Invalid enterprise scope for EnterpriseAdmin user {UserId}", _currentUserService.UserId);
                    return Result<PaginatedResult<GetViolationReportsResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }

                query = query.Where(x => x.InternshipGroup!.EnterpriseId.HasValue && x.InternshipGroup.EnterpriseId.Value == enterpriseId);
            }

            // 3) Search: match student full name or user code (case-insensitive).
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(x => x.Student.User.FullName.ToLower().Contains(term) || x.Student.User.UserCode.ToLower().Contains(term));
            }

            // 4) Filters: CreatedBy, Occurred date range, GroupId.
            if (request.CreatedById.HasValue) query = query.Where(x => x.CreatedBy == request.CreatedById.Value);
            if (request.OccurredFrom.HasValue) query = query.Where(x => x.OccurredDate >= request.OccurredFrom.Value);
            if (request.OccurredTo.HasValue) query = query.Where(x => x.OccurredDate <= request.OccurredTo.Value);
            if (request.GroupId.HasValue) query = query.Where(x => x.InternshipGroupId == request.GroupId.Value);

            // 5) Total count before pagination so frontend can show total items.
            var totalCount = await query.CountAsync(cancellationToken);

            // 6) If no results, return an empty paginated object (frontend expects pagination fields).
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

            // 7) Sorting: newest by default, client may request ascending creation ordering.
            var orderedQuery = request.OrderByCreatedAscending
                ? query.OrderBy(x => x.CreatedAt)
                : query.OrderByDescending(x => x.CreatedAt);

            // 8) Pagination + projection to DTO using AutoMapper.
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
    }
}
