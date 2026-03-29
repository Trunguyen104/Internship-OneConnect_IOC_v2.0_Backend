using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetPlacedStudents
{
    public class GetPlacedStudentsHandler : IRequestHandler<GetPlacedStudentsQuery, Result<PaginatedResult<GetPlacedStudentsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetPlacedStudentsHandler> _logger;

        public GetPlacedStudentsHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<GetPlacedStudentsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<PaginatedResult<GetPlacedStudentsResponse>>> Handle(GetPlacedStudentsQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<PaginatedResult<GetPlacedStudentsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
            {
                return Result<PaginatedResult<GetPlacedStudentsResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            var enterpriseId = enterpriseUser.EnterpriseId;

            List<Guid> resolvedPhaseIds;

            if (request.PhaseId.HasValue)
            {
                resolvedPhaseIds = new List<Guid> { request.PhaseId.Value };
            }
            else
            {
                resolvedPhaseIds = await ResolvePhaseIdsAsync(enterpriseId, cancellationToken);

                if (resolvedPhaseIds.Count == 0)
                {
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNoPhasesForEnterprise), enterpriseId);
                    return Result<PaginatedResult<GetPlacedStudentsResponse>>.Success(
                        new PaginatedResult<GetPlacedStudentsResponse>(new List<GetPlacedStudentsResponse>(), 0, request.PageNumber, request.PageSize));
                }
            }

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogResolvedPhases),
                resolvedPhaseIds.Count, enterpriseId, string.Join(", ", resolvedPhaseIds));

            // Fetch the display phase so unplaced students have phase context in the UI
            var displayPhase = await _unitOfWork.Repository<InternshipPhase>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => resolvedPhaseIds.Contains(p.PhaseId), cancellationToken);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var query = from app in _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
                        where app.EnterpriseId == enterpriseId
                           && app.Status == InternshipApplicationStatus.Approved

                        let assignedGroupMember = _unitOfWork.Repository<InternshipStudent>().Query()
                            .FirstOrDefault(m => m.StudentId == app.StudentId 
                                                 && m.InternshipGroup != null 
                                                 && m.InternshipGroup.EnterpriseId == enterpriseUser.EnterpriseId
                                                 && m.InternshipGroup.Status == GroupStatus.Active)
                        
                        let assignedGroup = assignedGroupMember != null ? assignedGroupMember.InternshipGroup : null
                        let phase = assignedGroup != null ? assignedGroup.InternshipPhase : null
                        
                        select new
                        {
                            App = app,
                            Student = app.Student,
                            User = app.Student!.User,
                            Phase = phase,
                            AssignedGroup = assignedGroup
                        };

            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                var lowerSearch = request.SearchTerm.ToLower();
                query = query.Where(q =>
                    q.User!.FullName.ToLower().Contains(lowerSearch) ||
                    q.User.UserCode.ToLower().Contains(lowerSearch) ||
                    (q.User.Email != null && q.User.Email.ToLower().Contains(lowerSearch)));
            }

            if (request.IsAssignedToGroup.HasValue)
            {
                query = request.IsAssignedToGroup.Value
                    ? query.Where(q => q.AssignedGroup != null)
                    : query.Where(q => q.AssignedGroup == null);
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var pagedData = await query
                .OrderByDescending(q => q.Phase != null ? q.Phase.StartDate : DateOnly.MinValue)
                .ThenByDescending(q => q.App.AppliedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(q => new GetPlacedStudentsResponse
                {
                    StudentId = q.Student!.StudentId,
                    StudentCode = q.User!.UserCode,
                    FullName = q.User.FullName,
                    Email = q.User.Email ?? string.Empty,
                    Major = q.Student.Major ?? string.Empty,
                    ClassName = q.Student.ClassName ?? string.Empty,
                    UniversityName = q.User.UniversityUser != null && q.User.UniversityUser.University != null
                        ? q.User.UniversityUser.University.Name
                        : null,

                    IsAssignedToGroup = q.AssignedGroup != null,
                    AssignedGroupId = q.AssignedGroup != null ? (Guid?)q.AssignedGroup.InternshipId : null,
                    AssignedGroupName = q.AssignedGroup != null ? q.AssignedGroup.GroupName : null,
                    MentorName = q.AssignedGroup != null
                        && q.AssignedGroup.Mentor != null
                        && q.AssignedGroup.Mentor.User != null
                        ? q.AssignedGroup.Mentor.User.FullName
                        : null,

                    PhaseId = q.Phase != null ? q.Phase.PhaseId : (displayPhase != null ? displayPhase.PhaseId : Guid.Empty),
                    PhaseName = q.Phase != null ? q.Phase.Name : (displayPhase != null ? displayPhase.Name : string.Empty),
                    PhaseStartDate = q.Phase != null ? q.Phase.StartDate : (displayPhase != null ? displayPhase.StartDate : default),
                    PhaseEndDate = q.Phase != null ? q.Phase.EndDate : (displayPhase != null ? displayPhase.EndDate : default),
                    PhaseStatus = q.Phase != null ? q.Phase.Status.ToString() : (displayPhase != null ? displayPhase.Status.ToString() : string.Empty)
                })
                .ToListAsync(cancellationToken);

            var paginatedResult = new PaginatedResult<GetPlacedStudentsResponse>(pagedData, totalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<GetPlacedStudentsResponse>>.Success(paginatedResult);
        }

        private async Task<List<Guid>> ResolvePhaseIdsAsync(Guid enterpriseId, CancellationToken cancellationToken)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            var activePhaseIds = await _unitOfWork.Repository<InternshipPhase>().Query()
                .AsNoTracking()
                .Where(p => p.EnterpriseId == enterpriseId 
                         && p.Status == InternshipPhaseStatus.Open 
                         && p.StartDate <= today 
                         && p.EndDate >= today)
                .Select(p => p.PhaseId)
                .ToListAsync(cancellationToken);

            if (activePhaseIds.Count > 0)
                return activePhaseIds;

            var nearestUpcomingPhaseId = await _unitOfWork.Repository<InternshipPhase>().Query()
                .AsNoTracking()
                .Where(p => p.EnterpriseId == enterpriseId 
                         && p.Status == InternshipPhaseStatus.Open 
                         && p.StartDate > today)
                .OrderBy(p => p.StartDate)
                .Select(p => p.PhaseId)
                .FirstOrDefaultAsync(cancellationToken);

            return nearestUpcomingPhaseId != Guid.Empty
                ? new List<Guid> { nearestUpcomingPhaseId }
                : new List<Guid>();
        }
    }
}
