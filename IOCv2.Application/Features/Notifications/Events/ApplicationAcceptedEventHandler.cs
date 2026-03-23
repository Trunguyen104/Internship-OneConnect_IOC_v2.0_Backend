using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Notifications.Events;

/// <summary>
/// Handler lắng nghe ApplicationAcceptedEvent:
/// 1. Insert Notification mới vào DB
/// 2. Đếm lại unread count của user
/// 3. Push real-time event qua INotificationPushService (SignalR)
/// </summary>
public class ApplicationAcceptedEventHandler : INotificationHandler<ApplicationAcceptedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPushService _pushService;
    private readonly ILogger<ApplicationAcceptedEventHandler> _logger;

    public ApplicationAcceptedEventHandler(
        IUnitOfWork unitOfWork,
        INotificationPushService pushService,
        ILogger<ApplicationAcceptedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _pushService = pushService;
        _logger = logger;
    }

    public async Task Handle(ApplicationAcceptedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling ApplicationAcceptedEvent for Student {StudentUserId}, ApplicationId {ApplicationId}",
            notification.StudentUserId, notification.ApplicationId);

        // 1. Tạo và lưu Notification vào DB
        var newNotification = new Notification
        {
            NotificationId = Guid.NewGuid(),
            UserId = notification.StudentUserId,
            Title = "Đơn thực tập đã được chấp nhận",
            Content = $"Chúc mừng! Đơn xin thực tập của bạn tại {notification.EnterpriseName} đã được chấp nhận.",
            Type = NotificationType.ApplicationAccepted,
            ReferenceType = nameof(InternshipApplication),
            ReferenceId = notification.ApplicationId,
            IsRead = false
        };

        await _unitOfWork.Repository<Notification>().AddAsync(newNotification, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        _logger.LogInformation("Notification {NotificationId} saved for user {UserId}",
            newNotification.NotificationId, notification.StudentUserId);

        // 2. Tính lại unread count
        var unreadCount = await _unitOfWork.Repository<Notification>()
            .CountAsync(n => n.UserId == notification.StudentUserId && !n.IsRead, cancellationToken);

        // 3. Push real-time event tới đúng user qua SignalR
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

        await _pushService.PushNewNotificationAsync(notification.StudentUserId, payload, cancellationToken);

        _logger.LogInformation(
            "Pushed ReceiveNewNotification to user {UserId}. UnreadCount: {Count}",
            notification.StudentUserId, unreadCount);
    }
}
