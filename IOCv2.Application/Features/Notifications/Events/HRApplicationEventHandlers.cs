using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Notifications.Events;

public class HRApplicationEventHandlers :
    INotificationHandler<ApplicationMovedToInterviewingEvent>,
    INotificationHandler<ApplicationOfferedEvent>,
    INotificationHandler<ApplicationPlacedSelfApplyEvent>,
    INotificationHandler<ApplicationRejectedSelfApplyEvent>,
    INotificationHandler<ApplicationPlacedUniAssignEvent>,
    INotificationHandler<ApplicationRejectedUniAssignEvent>,
    INotificationHandler<ApplicationApprovedNotifyUniAdminEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPushService _pushService;
    private readonly IMessageService _messageService;
    private readonly ILogger<HRApplicationEventHandlers> _logger;

    public HRApplicationEventHandlers(
        IUnitOfWork unitOfWork,
        INotificationPushService pushService,
        IMessageService messageService,
        ILogger<HRApplicationEventHandlers> logger)
    {
        _unitOfWork = unitOfWork;
        _pushService = pushService;
        _messageService = messageService;
        _logger = logger;
    }

    private async Task CreateAndPushNotificationAsync(
        Guid userId,
        string title,
        string content,
        NotificationType type,
        Guid? referenceId,
        CancellationToken cancellationToken)
    {
        // 1. Save to DB
        var newNotification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = userId,
            Title = title,
            Content = content,
            Type = type,
            ReferenceType = referenceId.HasValue ? nameof(InternshipApplication) : null,
            ReferenceId = referenceId,
            IsRead = false
        };

        await _unitOfWork.Repository<Notification>().AddAsync(newNotification, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        // 2. Count unread
        var unreadCount = await _unitOfWork.Repository<Notification>()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        // 3. Push via SignalR
        var payload = new
        {
            id = newNotification.NotificationId,
            title = newNotification.Title,
            content = newNotification.Content,
            type = newNotification.Type,
            referenceType = newNotification.ReferenceType,
            referenceId = newNotification.ReferenceId,
            isRead = newNotification.IsRead,
            currentUnreadCount = unreadCount,
            createdAt = newNotification.CreatedAt
        };

        await _pushService.PushNewNotificationAsync(userId, payload, cancellationToken);
    }

    public async Task Handle(ApplicationMovedToInterviewingEvent notification, CancellationToken cancellationToken)
    {
        var content = _messageService.GetMessage(MessageKeys.HRApplications.NotifyInterviewing)
            .Replace("{Enterprise}", notification.EnterpriseName);
        await CreateAndPushNotificationAsync(
            notification.StudentUserId,
            "Cập nhật trạng thái đơn ứng tuyển",
            content,
            NotificationType.ApplicationStatusChanged,
            notification.ApplicationId,
            cancellationToken);
    }

    public async Task Handle(ApplicationOfferedEvent notification, CancellationToken cancellationToken)
    {
        var content = _messageService.GetMessage(MessageKeys.HRApplications.NotifyOffered)
            .Replace("{Enterprise}", notification.EnterpriseName);
        await CreateAndPushNotificationAsync(
            notification.StudentUserId,
            "Bạn đã nhận được Offer",
            content,
            NotificationType.ApplicationStatusChanged,
            notification.ApplicationId,
            cancellationToken);
    }

    public async Task Handle(ApplicationPlacedSelfApplyEvent notification, CancellationToken cancellationToken)
    {
        var content = _messageService.GetMessage(MessageKeys.HRApplications.NotifyPlacedSelfApply)
            .Replace("{Enterprise}", notification.EnterpriseName);
        await CreateAndPushNotificationAsync(
            notification.StudentUserId,
            "Chúc mừng bạn đã được nhận",
            content,
            NotificationType.ApplicationStatusChanged,
            notification.ApplicationId,
            cancellationToken);
    }

    public async Task Handle(ApplicationRejectedSelfApplyEvent notification, CancellationToken cancellationToken)
    {
        var content = _messageService.GetMessage(MessageKeys.HRApplications.NotifyRejectedSelfApply)
            .Replace("{Enterprise}", notification.EnterpriseName);
        await CreateAndPushNotificationAsync(
            notification.StudentUserId,
            "Cập nhật hồ sơ ứng tuyển",
            content,
            NotificationType.ApplicationStatusChanged,
            notification.ApplicationId,
            cancellationToken);
    }

    public async Task Handle(ApplicationPlacedUniAssignEvent notification, CancellationToken cancellationToken)
    {
        var content = _messageService.GetMessage(MessageKeys.HRApplications.NotifyPlacedUniAssign)
            .Replace("{Enterprise}", notification.EnterpriseName);
        await CreateAndPushNotificationAsync(
            notification.StudentUserId,
            "Phê duyệt kết quả thực tập",
            content,
            NotificationType.ApplicationStatusChanged,
            notification.ApplicationId,
            cancellationToken);
    }

    public async Task Handle(ApplicationRejectedUniAssignEvent notification, CancellationToken cancellationToken)
    {
        // 1. Notify Student
        if (notification.StudentUserId != Guid.Empty)
        {
            var contentStudent = _messageService.GetMessage(MessageKeys.HRApplications.NotifyRejectedUniAssign)
                .Replace("{Enterprise}", notification.EnterpriseName);
            await CreateAndPushNotificationAsync(
                notification.StudentUserId,
                "Từ chối phân công thực tập",
                contentStudent,
                NotificationType.ApplicationStatusChanged,
                notification.ApplicationId,
                cancellationToken);
        }

        // 2. Notify Uni Admin (if we know the university)
        if (notification.UniversityId.HasValue)
        {
            // Get all active Uni Admins for this University
            var uniAdmins = await _unitOfWork.Repository<UniversityUser>().Query()
                .Where(u => u.UniversityId == notification.UniversityId.Value)
                .Select(u => u.UserId)
                .ToListAsync(cancellationToken);

            var contentUni = _messageService.GetMessage(MessageKeys.HRApplications.NotifyUniAdminRejected)
                .Replace("{Enterprise}", notification.EnterpriseName)
                .Replace("{StudentName}", notification.StudentName)
                .Replace("{Reason}", notification.RejectReason);

            foreach (var uniAdminUserId in uniAdmins)
            {
                await CreateAndPushNotificationAsync(
                    uniAdminUserId,
                    "Doanh nghiệp từ chối sinh viên",
                    contentUni,
                    NotificationType.ApplicationStatusChanged,
                    notification.ApplicationId,
                    cancellationToken);
            }
        }
    }


    public async Task Handle(ApplicationApprovedNotifyUniAdminEvent notification, CancellationToken cancellationToken)
    {
        if (notification.UniversityId.HasValue)
        {
            var uniAdmins = await _unitOfWork.Repository<UniversityUser>().Query()
                .Where(u => u.UniversityId == notification.UniversityId.Value)
                .Select(u => u.UserId)
                .ToListAsync(cancellationToken);

            var contentUni = _messageService.GetMessage(MessageKeys.HRApplications.NotifyUniAdminPlaced)
                .Replace("{Enterprise}", notification.EnterpriseName)
                .Replace("{StudentName}", notification.StudentName);

            foreach (var uniAdminUserId in uniAdmins)
            {
                await CreateAndPushNotificationAsync(
                    uniAdminUserId,
                    "Sinh viên được tiếp nhận thực tập",
                    contentUni,
                    NotificationType.ApplicationStatusChanged,
                    notification.ApplicationId,
                    cancellationToken);
            }
        }
    }
}
