using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Commands.CompleteProject
{
    public class CompleteProjectHandler : IRequestHandler<CompleteProjectCommand, Result<CompleteProjectResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IMessageService _message;
        private readonly ICacheService _cacheService;
        private readonly INotificationPushService _notificationService;
        private readonly ILogger<CompleteProjectHandler> _logger;

        public CompleteProjectHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IMessageService message,
            ICacheService cacheService,
            INotificationPushService notificationService,
            ILogger<CompleteProjectHandler> logger)
        {
            _unitOfWork           = unitOfWork;
            _currentUser          = currentUser;
            _message              = message;
            _cacheService         = cacheService;
            _notificationService  = notificationService;
            _logger               = logger;
        }

        public async Task<Result<CompleteProjectResponse>> Handle(CompleteProjectCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<CompleteProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<CompleteProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            var project = await _unitOfWork.Repository<Project>().Query()
                .Include(p => p.InternshipGroup)
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
                return Result<CompleteProjectResponse>.NotFound(_message.GetMessage(MessageKeys.Projects.NotFound));

            // Scope check
            if (project.MentorId != enterpriseUser.EnterpriseUserId &&
                project.InternshipGroup?.MentorId != enterpriseUser.EnterpriseUserId)
                return Result<CompleteProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            if (project.OperationalStatus != OperationalStatus.Active)
                return Result<CompleteProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.InvalidStatusForComplete), ResultErrorType.BadRequest);

            // Warning: số sinh viên đang thuộc group của project
            var pendingCount = 0;
            if (project.InternshipId.HasValue)
            {
                pendingCount = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .CountAsync(s => s.InternshipId == project.InternshipId.Value, cancellationToken);
            }

            // Warning: intern phase chưa kết thúc
            var internPhaseEndWarning = project.InternshipGroup?.EndDate > DateTime.UtcNow.Date;

            project.SetOperationalStatus(OperationalStatus.Completed);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

                _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogCompleteSuccess), project.ProjectId);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _message.GetMessage(MessageKeys.Projects.LogCompleteError), project.ProjectId);
                return Result<CompleteProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }

            // Post-commit: gửi notification đến Mentor nếu có
            if (project.MentorId.HasValue)
            {
                try
                {
                    var mentorEnterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(eu => eu.EnterpriseUserId == project.MentorId, cancellationToken);

                    var mentorUserId = mentorEnterpriseUser?.UserId;

                    if (mentorUserId.HasValue)
                    {
                        var notification = new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId         = mentorUserId.Value,
                            Title          = _message.GetMessage(MessageKeys.Projects.NotifCompletedTitle),
                            Content        = _message.GetMessage(MessageKeys.Projects.NotifCompletedContent, project.ProjectId),
                            Type           = NotificationType.General,
                            ReferenceType  = "Project",
                            ReferenceId    = project.ProjectId,
                            IsRead         = false
                        };

                        await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
                        await _unitOfWork.SaveChangeAsync(cancellationToken);

                        var unreadCount = await _unitOfWork.Repository<Notification>()
                            .CountAsync(n => n.UserId == mentorUserId.Value && !n.IsRead, cancellationToken);

                        await _notificationService.PushNewNotificationAsync(mentorUserId.Value, new
                        {
                            type             = NotificationType.General,
                            referenceType    = "Project",
                            referenceId      = project.ProjectId,
                            currentUnreadCount = unreadCount
                        }, cancellationToken);
                    }
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx,
                        _message.GetMessage(MessageKeys.Projects.LogCompleteNotificationFailed),
                        project.ProjectId);
                }
            }

            // AC-13: Push ProjectListChanged signal tới Mentor
            if (Guid.TryParse(_currentUser.UserId, out var mentorUserIdForSignal))
            {
                try
                {
                    await _notificationService.PushNewNotificationAsync(mentorUserIdForSignal, new
                    {
                        type      = ProjectSignalConstants.ProjectListChanged,
                        action    = ProjectSignalConstants.Actions.Completed,
                        projectId = project.ProjectId
                    }, cancellationToken);
                    _logger.LogInformation(
                        _message.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                        ProjectSignalConstants.Actions.Completed, mentorUserIdForSignal, project.ProjectId);
                }
                catch (Exception signalEx)
                {
                    _logger.LogWarning(signalEx, _message.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                        ProjectSignalConstants.Actions.Completed, mentorUserIdForSignal, project.ProjectId);
                }
            }

            return Result<CompleteProjectResponse>.Success(new CompleteProjectResponse
            {
                ProjectId              = project.ProjectId,
                OperationalStatus      = OperationalStatus.Completed,
                PendingStudentsCount   = pendingCount,
                InternPhaseEndWarning  = internPhaseEndWarning,
                UpdatedAt              = project.UpdatedAt ?? DateTime.UtcNow
            });
        }
    }
}
