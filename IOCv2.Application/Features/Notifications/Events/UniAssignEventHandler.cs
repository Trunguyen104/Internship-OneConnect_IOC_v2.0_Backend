using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Notifications.Events;

internal class UniAssignEventHandler :
    INotificationHandler<ApplicationAssignedUniAssignEvent>,
    INotificationHandler<ApplicationReassignedFromPendingEvent>,
    INotificationHandler<ApplicationReassignedFromPlacedEvent>,
    INotificationHandler<ApplicationUnassignedUniAssignEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPushService _pushService;
    private readonly ILogger<UniAssignEventHandler> _logger;

    public UniAssignEventHandler(
        IUnitOfWork unitOfWork,
        INotificationPushService pushService,
        ILogger<UniAssignEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _pushService = pushService;
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

        var unreadCount = await _unitOfWork.Repository<Notification>()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

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

    public async Task Handle(ApplicationAssignedUniAssignEvent notification, CancellationToken cancellationToken)
    {
        var title = "Bạn đã được chỉ định thực tập";
        var content = $"Bạn đã được chỉ định vào {notification.EnterpriseName} cho kỳ {notification.TermName}. Đang chờ doanh nghiệp xác nhận.";
        await CreateAndPushNotificationAsync(
            notification.StudentUserId,
            title,
            content,
            NotificationType.ApplicationStatusChanged,
            notification.ApplicationId,
            cancellationToken);
    }

    public async Task Handle(ApplicationReassignedFromPendingEvent notification, CancellationToken cancellationToken)
    {
        var title = "Cập nhật chỉ định thực tập";
        var content = $"Chỉ định thực tập của bạn đã được cập nhật. Enterprise mới: {notification.NewEnterpriseName}. Đang chờ doanh nghiệp xác nhận.";
        await CreateAndPushNotificationAsync(
            notification.StudentUserId,
            title,
            content,
            NotificationType.ApplicationStatusChanged,
            notification.ApplicationId,
            cancellationToken);
    }

    public async Task Handle(ApplicationReassignedFromPlacedEvent notification, CancellationToken cancellationToken)
    {
        var title = "Chỉ định thực tập đã thay đổi";
        var content = $"Chỉ định thực tập của bạn tại {notification.OldEnterpriseName} đã bị hủy. Bạn vừa được chỉ định đến {notification.NewEnterpriseName} — đang chờ doanh nghiệp xác nhận.";
        await CreateAndPushNotificationAsync(
            notification.StudentUserId,
            title,
            content,
            NotificationType.ApplicationStatusChanged,
            notification.ApplicationId,
            cancellationToken);
    }

    public async Task Handle(ApplicationUnassignedUniAssignEvent notification, CancellationToken cancellationToken)
    {
        var title = "Thông báo hủy chỉ định thực tập";
        var content = $"Chỉ định thực tập của bạn trong kỳ {notification.TermName} đã bị hủy. Vui lòng liên hệ Uni Admin để được hỗ trợ.";
        await CreateAndPushNotificationAsync(
            notification.StudentUserId,
            title,
            content,
            NotificationType.ApplicationStatusChanged,
            notification.ApplicationId,
            cancellationToken);
    }
}