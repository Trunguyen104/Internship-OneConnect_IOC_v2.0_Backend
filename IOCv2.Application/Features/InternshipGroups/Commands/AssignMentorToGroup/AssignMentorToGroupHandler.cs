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

namespace IOCv2.Application.Features.InternshipGroups.Commands.AssignMentorToGroup;

public class AssignMentorToGroupHandler
    : IRequestHandler<AssignMentorToGroupCommand, Result<AssignMentorToGroupResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ICacheService _cacheService;
    private readonly INotificationPushService _pushService;
    private readonly ILogger<AssignMentorToGroupHandler> _logger;

    public AssignMentorToGroupHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ICacheService cacheService,
        INotificationPushService pushService,
        ILogger<AssignMentorToGroupHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _cacheService = cacheService;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task<Result<AssignMentorToGroupResponse>> Handle(
        AssignMentorToGroupCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Auth
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<AssignMentorToGroupResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                ResultErrorType.Unauthorized);

        var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

        if (enterpriseUser == null)
            return Result<AssignMentorToGroupResponse>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                ResultErrorType.Forbidden);

        // 2. Load group
        var group = await _unitOfWork.Repository<InternshipGroup>().Query()
            .FirstOrDefaultAsync(g => g.InternshipId == request.InternshipGroupId && g.DeletedAt == null, cancellationToken);

        if (group == null)
            return Result<AssignMentorToGroupResponse>.NotFound(
                _messageService.GetMessage(MessageKeys.InternshipGroups.AssignMentorGroupNotFound));

        if (group.EnterpriseId != enterpriseUser.EnterpriseId)
            return Result<AssignMentorToGroupResponse>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise),
                ResultErrorType.Forbidden);

        if (group.Status != GroupStatus.Active)
            return Result<AssignMentorToGroupResponse>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipGroups.AssignMentorGroupNotActive),
                ResultErrorType.BadRequest);

        // 3. Resolve mentor (frontend gửi UserId)
        var mentor = await _unitOfWork.Repository<EnterpriseUser>().Query()
            .Include(eu => eu.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(eu => eu.UserId == request.MentorUserId, cancellationToken);

        if (mentor == null)
            return Result<AssignMentorToGroupResponse>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipGroups.MentorNotFound),
                ResultErrorType.NotFound);

        if (mentor.User.Role != UserRole.Mentor)
        {
            _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogMentorRoleInvalid),
                request.MentorUserId);
            return Result<AssignMentorToGroupResponse>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipGroups.MentorNotFound),
                ResultErrorType.BadRequest);
        }

        if (mentor.EnterpriseId != group.EnterpriseId)
            return Result<AssignMentorToGroupResponse>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise),
                ResultErrorType.BadRequest);

        // Kiểm tra same mentor (no-op)
        if (mentor.EnterpriseUserId == group.MentorId)
        {
            _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogAssignMentorSameMentor),
                request.MentorUserId, request.InternshipGroupId);
            return Result<AssignMentorToGroupResponse>.Failure(
                _messageService.GetMessage(MessageKeys.InternshipGroups.AssignMentorSameMentor),
                ResultErrorType.BadRequest);
        }

        // 4. Xác định loại action
        var isFirstAssign = !group.MentorId.HasValue;
        var actionType = isFirstAssign ? MentorActionType.Assign : MentorActionType.Change;
        var oldMentorId = group.MentorId;

        // Load project IDs để cập nhật MentorId sau
        var projectIds = await _unitOfWork.Repository<Project>().Query()
            .AsNoTracking()
            .Where(p => p.InternshipId == group.InternshipId && p.OperationalStatus == OperationalStatus.Active)
            .Select(p => p.ProjectId)
            .ToListAsync(cancellationToken);

        // 5. Transaction: cập nhật group + projects + ghi history
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            group.AssignMentor(mentor.EnterpriseUserId);
            await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(group);

            // Cập nhật mentor trên tất cả Active projects của group
            if (projectIds.Any())
            {
                await _unitOfWork.Repository<Project>().ExecuteUpdateAsync(
                    p => p.InternshipId == group.InternshipId && p.OperationalStatus == OperationalStatus.Active,
                    s => s.SetProperty(p => p.MentorId, mentor.EnterpriseUserId)
                          .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);
            }

            var historyRecord = GroupMentorHistory.Create(
                group.InternshipId, oldMentorId, mentor.EnterpriseUserId, currentUserId, actionType);
            await _unitOfWork.Repository<GroupMentorHistory>().AddAsync(historyRecord, cancellationToken);

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogAssignMentorFailed), group.InternshipId);
            return Result<AssignMentorToGroupResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError),
                ResultErrorType.InternalServerError);
        }

        // 6. Cache invalidation
        await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(group.InternshipId), cancellationToken);
        await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);
        if (projectIds.Any())
        {
            await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);
            foreach (var projectId in projectIds)
                await _cacheService.RemoveAsync(ProjectCacheKeys.Project(projectId), cancellationToken);
        }

        // 7. Notifications (post-commit, lỗi không rollback)
        try
        {
            // Load old mentor nếu cần (cho AC-05)
            EnterpriseUser? oldMentor = null;
            if (!isFirstAssign && oldMentorId.HasValue)
            {
                oldMentor = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .Include(eu => eu.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.EnterpriseUserId == oldMentorId.Value, cancellationToken);
            }

            // Load sinh viên trong nhóm
            var memberStudentUserIds = await _unitOfWork.Repository<InternshipStudent>().Query()
                .AsNoTracking()
                .Where(m => m.InternshipId == group.InternshipId && m.DeletedAt == null)
                .Select(m => m.Student.UserId)
                .ToListAsync(cancellationToken);

            var memberCount = memberStudentUserIds.Count;
            var notAvailable = _messageService.GetMessage(MessageKeys.InternshipGroups.Unassigned);
            var startDateStr = group.StartDate.HasValue
                ? group.StartDate.Value.ToString("dd/MM/yyyy") : notAvailable;
            var endDateStr = group.EndDate.HasValue
                ? group.EndDate.Value.ToString("dd/MM/yyyy") : notAvailable;

            var notifications = new List<Notification>();

            if (isFirstAssign)
            {
                // AC-04: Gán lần đầu — notify mentor mới
                notifications.Add(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = mentor.UserId,
                    Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationMentorAssignedFirstTitle),
                    Content = _messageService.GetMessage(
                        MessageKeys.InternshipGroups.NotificationMentorAssignedFirstContent,
                        group.GroupName, memberCount, startDateStr, endDateStr),
                    Type = NotificationType.General,
                    ReferenceType = nameof(InternshipGroup),
                    ReferenceId = group.InternshipId,
                    IsRead = false
                });

                // AC-04: Notify từng sinh viên
                foreach (var studentUserId in memberStudentUserIds)
                {
                    notifications.Add(new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = studentUserId,
                        Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationStudentMentorAssignedTitle),
                        Content = _messageService.GetMessage(
                            MessageKeys.InternshipGroups.NotificationStudentMentorAssignedContent,
                            group.GroupName, mentor.User.FullName, mentor.User.Email),
                        Type = NotificationType.General,
                        ReferenceType = nameof(InternshipGroup),
                        ReferenceId = group.InternshipId,
                        IsRead = false
                    });
                }
            }
            else
            {
                // AC-05: Đổi mentor — notify mentor cũ
                if (oldMentor != null)
                {
                    notifications.Add(new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = oldMentor.UserId,
                        Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationMentorReplacedOldTitle),
                        Content = _messageService.GetMessage(
                            MessageKeys.InternshipGroups.NotificationMentorReplacedOldContent,
                            group.GroupName),
                        Type = NotificationType.General,
                        ReferenceType = nameof(InternshipGroup),
                        ReferenceId = group.InternshipId,
                        IsRead = false
                    });
                }

                // AC-05: Notify mentor mới
                var oldMentorName = oldMentor?.User?.FullName
                    ?? _messageService.GetMessage(MessageKeys.InternshipGroups.Unassigned);
                notifications.Add(new Notification
                {
                    NotificationId = Guid.NewGuid(),
                    UserId = mentor.UserId,
                    Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationMentorReplacedNewTitle),
                    Content = _messageService.GetMessage(
                        MessageKeys.InternshipGroups.NotificationMentorReplacedNewContent,
                        group.GroupName, oldMentorName),
                    Type = NotificationType.General,
                    ReferenceType = nameof(InternshipGroup),
                    ReferenceId = group.InternshipId,
                    IsRead = false
                });

                // AC-05: Notify từng sinh viên
                foreach (var studentUserId in memberStudentUserIds)
                {
                    notifications.Add(new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = studentUserId,
                        Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationStudentMentorChangedTitle),
                        Content = _messageService.GetMessage(
                            MessageKeys.InternshipGroups.NotificationStudentMentorChangedContent,
                            group.GroupName, mentor.User.FullName, mentor.User.Email),
                        Type = NotificationType.General,
                        ReferenceType = nameof(InternshipGroup),
                        ReferenceId = group.InternshipId,
                        IsRead = false
                    });
                }
            }

            foreach (var notification in notifications)
                await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);

            if (notifications.Any())
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                foreach (var recipientUserId in notifications.Select(n => n.UserId).Distinct())
                {
                    var unreadCount = await _unitOfWork.Repository<Notification>()
                        .CountAsync(n => n.UserId == recipientUserId && !n.IsRead, cancellationToken);

                    await _pushService.PushNewNotificationAsync(recipientUserId, new
                    {
                        type = NotificationType.General,
                        referenceType = nameof(InternshipGroup),
                        referenceId = group.InternshipId,
                        currentUnreadCount = unreadCount
                    }, cancellationToken);
                }
            }
        }
        catch (Exception notifyEx)
        {
            _logger.LogWarning(notifyEx,
                _messageService.GetMessage(MessageKeys.InternshipGroups.LogAssignMentorNotifyFailed),
                group.InternshipId);
        }

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipGroups.LogAssignMentorSuccess),
            mentor.User.FullName, group.InternshipId);

        return Result<AssignMentorToGroupResponse>.Success(
            new AssignMentorToGroupResponse
            {
                InternshipGroupId = group.InternshipId,
                MentorEnterpriseUserId = mentor.EnterpriseUserId,
                MentorUserId = mentor.UserId,
                MentorFullName = mentor.User.FullName,
                MentorEmail = mentor.User.Email,
                ActionType = actionType.ToString(),
                UpdatedAt = group.UpdatedAt ?? DateTime.UtcNow
            },
            _messageService.GetMessage(MessageKeys.InternshipGroups.AssignMentorSuccess));
    }
}
