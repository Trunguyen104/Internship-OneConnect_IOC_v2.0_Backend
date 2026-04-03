using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId
{
    public class GetProjectsByStudentIdHandler : IRequestHandler<GetProjectsByStudentIdQuery, Result<PaginatedResult<GetProjectsByStudentIdResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetProjectsByStudentIdHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public GetProjectsByStudentIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetProjectsByStudentIdHandler> logger,
            ICurrentUserService currentUserService,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<PaginatedResult<GetProjectsByStudentIdResponse>>> Handle(
            GetProjectsByStudentIdQuery request,
            CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
                return Result<PaginatedResult<GetProjectsByStudentIdResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var studentId = await _unitOfWork.Repository<Student>().Query().Where(s => s.UserId == userId).Select(s => s.StudentId).FirstOrDefaultAsync(cancellationToken);

            // Student visibility: only Published + (Active|Completed).
            IQueryable<Project> query = _unitOfWork.Repository<Project>().Query()
                .Where(p => p.InternshipId != null
                         && p.InternshipGroup != null
                         && p.InternshipGroup.Members.Any(s => s.StudentId == studentId)
                         && p.VisibilityStatus == VisibilityStatus.Published
                         && (p.OperationalStatus == OperationalStatus.Active || p.OperationalStatus == OperationalStatus.Completed))
                .AsNoTracking();

            if (request.VisibilityStatus.HasValue)
                query = query.Where(p => p.VisibilityStatus == request.VisibilityStatus.Value);

            if (request.OperationalStatus.HasValue)
                query = query.Where(p => p.OperationalStatus == request.OperationalStatus.Value);

            // Apply search term
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(p =>
                    p.ProjectName.ToLower().Contains(term) ||
                    (p.Description != null && p.Description.ToLower().Contains(term)));
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply sorting
            query = ApplySorting(query, request.SortColumn, request.SortOrder);

            // Apply pagination
            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<GetProjectsByStudentIdResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            // Create paginated result
            var result = PaginatedResult<GetProjectsByStudentIdResponse>.Create(
                items, totalCount, request.PageNumber, request.PageSize);

            return Result<PaginatedResult<GetProjectsByStudentIdResponse>>.Success(result);
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

                _ => query.OrderByDescending(p => p.CreatedAt) // Default sorting
            };
        }

    }
}
