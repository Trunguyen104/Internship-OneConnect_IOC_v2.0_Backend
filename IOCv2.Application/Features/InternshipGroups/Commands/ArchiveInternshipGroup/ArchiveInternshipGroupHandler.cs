using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Common;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Commands.ArchiveInternshipGroup
{
    public class ArchiveInternshipGroupHandler : IRequestHandler<ArchiveInternshipGroupCommand, Result<ArchiveInternshipGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<ArchiveInternshipGroupHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly INotificationPushService _pushService;

        public ArchiveInternshipGroupHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<ArchiveInternshipGroupHandler> logger,
            ICacheService cacheService,
            INotificationPushService pushService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
            _cacheService = cacheService;
            _pushService = pushService;
        }

        public async Task<Result<ArchiveInternshipGroupResponse>> Handle(ArchiveInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
            {
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Mentor)
                .FirstOrDefaultAsync(g => g.InternshipId == request.InternshipGroupId && g.DeletedAt == null, cancellationToken);

            if (group == null)
            {
                return Result<ArchiveInternshipGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
            }

            if (group.EnterpriseId != enterpriseUser.EnterpriseId)
            {
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise),
                    ResultErrorType.Forbidden);
            }

            if (group.Status == GroupStatus.Archived)
            {
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.GroupAlreadyArchived),
                    ResultErrorType.BadRequest);
            }

            group.UpdateStatus(GroupStatus.Archived);

            var projectIdsInGroup = await _unitOfWork.Repository<Project>().Query()
                .Where(p => p.InternshipId == request.InternshipGroupId)
                .Select(p => p.ProjectId)
                .ToListAsync(cancellationToken);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(group);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogArchiveError));
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }

            // Post-commit cache invalidation
            await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(request.InternshipGroupId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);
            await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);
            foreach (var projectId in projectIdsInGroup)
                await _cacheService.RemoveAsync(ProjectCacheKeys.Project(projectId), cancellationToken);

            if (group.MentorId.HasValue && group.Mentor != null)
            {
                try
                {
                    var mentorUserId = group.Mentor.UserId;
                    var notification = new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = mentorUserId,
                        Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationGroupArchivedTitle),
                        Content = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationGroupArchivedContent, group.GroupName),
                        Type = NotificationType.General,
                        ReferenceType = nameof(InternshipGroup),
                        ReferenceId = group.InternshipId,
                        IsRead = false
                    };

                    await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
                    await _unitOfWork.SaveChangeAsync(cancellationToken);

                    var unreadCount = await _unitOfWork.Repository<Notification>()
                        .CountAsync(n => n.UserId == mentorUserId && !n.IsRead, cancellationToken);

                    await _pushService.PushNewNotificationAsync(mentorUserId, new
                    {
                        type = NotificationType.General,
                        referenceType = nameof(InternshipGroup),
                        referenceId = group.InternshipId,
                        currentUnreadCount = unreadCount
                    }, cancellationToken);
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, _messageService.GetMessage(MessageKeys.InternshipGroups.LogArchiveNotifyFailed), request.InternshipGroupId);
                }
            }

            return Result<ArchiveInternshipGroupResponse>.Success(
                new ArchiveInternshipGroupResponse { InternshipGroupId = request.InternshipGroupId },
                _messageService.GetMessage(MessageKeys.InternshipGroups.ArchiveSuccess));
        }
    }
}
