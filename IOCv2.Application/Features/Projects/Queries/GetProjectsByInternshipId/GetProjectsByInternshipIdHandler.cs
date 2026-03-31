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

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByInternshipId
{
    public class GetProjectsByInternshipIdHandler : IRequestHandler<GetProjectsByInternshipIdQuery, Result<PaginatedResult<GetProjectsByInternshipIdResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetProjectsByInternshipIdHandler> _logger;
        private readonly ICurrentUserService? _currentUserService;

        public GetProjectsByInternshipIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetProjectsByInternshipIdHandler> logger,
            ICurrentUserService? currentUserService = null)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<PaginatedResult<GetProjectsByInternshipIdResponse>>> Handle(GetProjectsByInternshipIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogGetByInternshipId), request.InternshipId);

            // Check if the internship exists
            var internshipExists = await _unitOfWork.Repository<InternshipGroup>()
                .ExistsAsync(i => i.InternshipId == request.InternshipId, cancellationToken);
            
            if (!internshipExists) 
            { 
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                return Result<PaginatedResult<GetProjectsByInternshipIdResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Internships.NotFound), ResultErrorType.NotFound); 
            }

                // Base query
                var query = _unitOfWork.Repository<Project>().Query()
                .Where(p => p.InternshipId == request.InternshipId)
                .AsNoTracking();

                var isMentor = string.Equals(_currentUserService?.Role, "Mentor", StringComparison.OrdinalIgnoreCase);
                var isStudent = string.Equals(_currentUserService?.Role, "Student", StringComparison.OrdinalIgnoreCase);
                if (isMentor)
                {
                    if (!Guid.TryParse(_currentUserService?.UserId, out var currentMentorUserId))
                        return Result<PaginatedResult<GetProjectsByInternshipIdResponse>>.Failure(
                            _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

                    var mentorEnterpriseUserId = await _unitOfWork.Repository<EnterpriseUser>().Query()
                        .AsNoTracking()
                        .Where(eu => eu.UserId == currentMentorUserId)
                        .Select(eu => eu.EnterpriseUserId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (mentorEnterpriseUserId == Guid.Empty)
                        return Result<PaginatedResult<GetProjectsByInternshipIdResponse>>.Failure(
                            _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                    var isMentorOfGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                        .AsNoTracking()
                        .AnyAsync(g => g.InternshipId == request.InternshipId && g.MentorId == mentorEnterpriseUserId, cancellationToken);

                    if (!isMentorOfGroup)
                        return Result<PaginatedResult<GetProjectsByInternshipIdResponse>>.Failure(
                            _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
                }

                if (isStudent)
                {
                    if (!Guid.TryParse(_currentUserService?.UserId, out var currentUserId))
                        return Result<PaginatedResult<GetProjectsByInternshipIdResponse>>.Failure(
                            _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

                    var studentId = await _unitOfWork.Repository<Student>().Query()
                        .AsNoTracking()
                        .Where(s => s.UserId == currentUserId)
                        .Select(s => s.StudentId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (studentId == Guid.Empty)
                        return Result<PaginatedResult<GetProjectsByInternshipIdResponse>>.Failure(
                            _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                    var isMemberOfGroup = await _unitOfWork.Repository<InternshipStudent>().Query()
                        .AsNoTracking()
                        .AnyAsync(s => s.InternshipId == request.InternshipId && s.StudentId == studentId, cancellationToken);

                    if (!isMemberOfGroup)
                        return Result<PaginatedResult<GetProjectsByInternshipIdResponse>>.Failure(
                            _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                    query = query.Where(p => p.VisibilityStatus == VisibilityStatus.Published &&
                                             (p.OperationalStatus == OperationalStatus.Active ||
                                              p.OperationalStatus == OperationalStatus.Completed));
                }

                // Apply search term
                if (!string.IsNullOrWhiteSpace(request.SearchTerm)) { var term = request.SearchTerm.Trim().ToLower(); 
                    query = query.Where(p => p.ProjectName.ToLower().Contains(term) || (p.Description != null && p.Description.ToLower().Contains(term))); }

                // Apply two-layer status filters
                if (request.VisibilityStatus.HasValue)
                {
                    query = query.Where(p => p.VisibilityStatus == request.VisibilityStatus.Value);
                }

                if (request.OperationalStatus.HasValue)
                {
                    query = query.Where(p => p.OperationalStatus == request.OperationalStatus.Value);
                }

                // Apply date range filter
                if (request.FromDate.HasValue) { query = query.Where(p => p.StartDate >= request.FromDate.Value); }
                if (request.ToDate.HasValue) { query = query.Where(p => p.EndDate <= request.ToDate.Value); }

                // Get total count before pagination
                var totalCount = await query.CountAsync(cancellationToken);

                // Apply sorting
                query = ApplySorting(query, request.SortColumn, request.SortOrder);

                // Apply pagination and project to response
                var items = await query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
                .ProjectTo<GetProjectsByInternshipIdResponse>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken);
                var result = PaginatedResult<GetProjectsByInternshipIdResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);
                
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogGetByInternshipIdSuccess), items.Count, request.InternshipId);

                return Result<PaginatedResult<GetProjectsByInternshipIdResponse>>.Success(result);
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
