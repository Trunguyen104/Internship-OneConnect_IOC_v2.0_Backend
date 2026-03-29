using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Commands.SwapGroup
{
    public class SwapGroupHandler : IRequestHandler<SwapGroupCommand, Result<SwapGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IMessageService _message;
        private readonly ICacheService _cacheService;
        private readonly ILogger<SwapGroupHandler> _logger;
        private readonly INotificationPushService _pushService;

        public SwapGroupHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser,
            IMessageService message, ICacheService cacheService,
            ILogger<SwapGroupHandler> logger, INotificationPushService pushService)
        {
            _unitOfWork   = unitOfWork;
            _currentUser  = currentUser;
            _message      = message;
            _cacheService = cacheService;
            _logger       = logger;
            _pushService  = pushService;
        }

        public async Task<Result<SwapGroupResponse>> Handle(SwapGroupCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<SwapGroupResponse>.Failure(_message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<SwapGroupResponse>.Failure(_message.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            var project = await _unitOfWork.Repository<Project>().Query()
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
                return Result<SwapGroupResponse>.NotFound(_message.GetMessage(MessageKeys.Projects.NotFound));

            // Scope: chỉ Mentor tạo project
            if (project.MentorId != enterpriseUser.EnterpriseUserId)
                return Result<SwapGroupResponse>.Failure(_message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            // Project phải đang Active (có group)
            if (project.OperationalStatus != OperationalStatus.Active)
                return Result<SwapGroupResponse>.Failure(_message.GetMessage(MessageKeys.Projects.ProjectNotAssigned), ResultErrorType.BadRequest);

            // Student data check: block swap nếu có WorkItem hoặc Sprint
            if (project.InternshipId.HasValue)
            {
                var hasWorkItems = await _unitOfWork.Repository<WorkItem>().Query()
                    .AnyAsync(w => w.ProjectId == project.ProjectId, cancellationToken);

                if (hasWorkItems)
                    return Result<SwapGroupResponse>.Failure(_message.GetMessage(MessageKeys.Projects.HasStudentDataWorkItems), ResultErrorType.BadRequest);

                var hasSprints = await _unitOfWork.Repository<Sprint>().Query()
                    .AnyAsync(s => s.ProjectId == project.ProjectId, cancellationToken);

                if (hasSprints)
                    return Result<SwapGroupResponse>.Failure(_message.GetMessage(MessageKeys.Projects.HasStudentDataSprints), ResultErrorType.BadRequest);
            }

            // Load new group
            var newGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                .FirstOrDefaultAsync(g => g.InternshipId == request.NewInternshipId, cancellationToken);

            if (newGroup == null)
                return Result<SwapGroupResponse>.NotFound(_message.GetMessage(MessageKeys.Internships.NotFound));

            if (newGroup.Status == GroupStatus.Archived || newGroup.Status == GroupStatus.Finished)
                return Result<SwapGroupResponse>.Failure(_message.GetMessage(MessageKeys.Projects.GroupNotActive), ResultErrorType.BadRequest);

            if (newGroup.EndDate.HasValue && newGroup.EndDate.Value.Date < DateTime.UtcNow.Date)
                return Result<SwapGroupResponse>.Failure(_message.GetMessage(MessageKeys.Projects.GroupPhaseEnded), ResultErrorType.BadRequest);

            // Mentor phải phụ trách group mới
            if (newGroup.MentorId != enterpriseUser.EnterpriseUserId)
                return Result<SwapGroupResponse>.Failure(_message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            var oldInternshipId = project.InternshipId;

            project.SwapGroup(newGroup.InternshipId, newGroup.StartDate, newGroup.EndDate);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _message.GetMessage(MessageKeys.Projects.LogSwapGroupError), project.ProjectId);
                return Result<SwapGroupResponse>.Failure(_message.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }

            // Notify students nếu project đã Published
            if (project.VisibilityStatus == VisibilityStatus.Published)
            {
                try
                {
                    // Notify students của group cũ
                    if (oldInternshipId.HasValue)
                    {
                        var oldStudentUserIds = await _unitOfWork.Repository<InternshipStudent>().Query()
                            .Where(s => s.InternshipId == oldInternshipId.Value)
                            .Select(s => s.Student.UserId)
                            .ToListAsync(cancellationToken);

                        foreach (var userId in oldStudentUserIds)
                        {
                            var notif = new Notification
                            {
                                NotificationId = Guid.NewGuid(),
                                UserId         = userId,
                                Title          = _message.GetMessage(MessageKeys.Projects.NotifProjectLeftTitle),
                                Content        = _message.GetMessage(MessageKeys.Projects.NotifProjectLeftContent, project.ProjectName),
                                Type           = NotificationType.General,
                                ReferenceType  = "Project",
                                ReferenceId    = project.ProjectId
                            };
                            await _unitOfWork.Repository<Notification>().AddAsync(notif, cancellationToken);
                        }

                        if (oldStudentUserIds.Any())
                        {
                            await _unitOfWork.SaveChangeAsync(cancellationToken);
                            foreach (var userId in oldStudentUserIds)
                            {
                                var unreadCount = await _unitOfWork.Repository<Notification>()
                                    .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
                                await _pushService.PushNewNotificationAsync(userId, new
                                {
                                    type               = NotificationType.General,
                                    referenceType      = "Project",
                                    referenceId        = project.ProjectId,
                                    currentUnreadCount = unreadCount
                                }, cancellationToken);
                            }
                        }
                    }

                    // Notify students của group mới
                    var newStudentUserIds = await _unitOfWork.Repository<InternshipStudent>().Query()
                        .Where(s => s.InternshipId == newGroup.InternshipId)
                        .Select(s => s.Student.UserId)
                        .ToListAsync(cancellationToken);

                    foreach (var userId in newStudentUserIds)
                    {
                        var notif = new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId         = userId,
                            Title          = _message.GetMessage(MessageKeys.Projects.NotifNewProjectTitle),
                            Content        = _message.GetMessage(MessageKeys.Projects.NotifNewProjectContent, project.ProjectName),
                            Type           = NotificationType.General,
                            ReferenceType  = "Project",
                            ReferenceId    = project.ProjectId
                        };
                        await _unitOfWork.Repository<Notification>().AddAsync(notif, cancellationToken);
                    }

                    if (newStudentUserIds.Any())
                    {
                        await _unitOfWork.SaveChangeAsync(cancellationToken);
                        foreach (var userId in newStudentUserIds)
                        {
                            var unreadCount = await _unitOfWork.Repository<Notification>()
                                .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
                            await _pushService.PushNewNotificationAsync(userId, new
                            {
                                type               = NotificationType.General,
                                referenceType      = "Project",
                                referenceId        = project.ProjectId,
                                currentUnreadCount = unreadCount
                            }, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, _message.GetMessage(MessageKeys.Projects.LogSwapNotificationFailed), project.ProjectId);
                }
            }

            await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

            _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogSwapGroupSuccess),
                project.ProjectId, oldInternshipId, newGroup.InternshipId);

            // AC-13: Push ProjectListChanged signal tới Mentor
            if (Guid.TryParse(_currentUser.UserId, out var mentorUserIdForSignal))
            {
                try
                {
                    await _pushService.PushNewNotificationAsync(mentorUserIdForSignal, new
                    {
                        type      = ProjectSignalConstants.ProjectListChanged,
                        action    = ProjectSignalConstants.Actions.GroupSwapped,
                        projectId = project.ProjectId
                    }, cancellationToken);
                    _logger.LogInformation(
                        _message.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                        ProjectSignalConstants.Actions.GroupSwapped, mentorUserIdForSignal, project.ProjectId);
                }
                catch (Exception signalEx)
                {
                    _logger.LogWarning(signalEx, _message.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                        ProjectSignalConstants.Actions.GroupSwapped, mentorUserIdForSignal, project.ProjectId);
                }
            }

            return Result<SwapGroupResponse>.Success(new SwapGroupResponse
            {
                ProjectId         = project.ProjectId,
                InternshipId      = newGroup.InternshipId,
                VisibilityStatus  = project.VisibilityStatus,
                OperationalStatus = project.OperationalStatus,
                StartDate         = project.StartDate,
                EndDate           = project.EndDate,
                UpdatedAt         = project.UpdatedAt ?? DateTime.UtcNow
            });
        }
    }
}
