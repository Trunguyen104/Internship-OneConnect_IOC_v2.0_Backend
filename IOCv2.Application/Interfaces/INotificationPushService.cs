namespace IOCv2.Application.Interfaces;

/// <summary>
/// Interface để push real-time notification tới client qua SignalR.
/// Được implement ở Infrastructure layer để Application không phụ thuộc SignalR.
/// </summary>
public interface INotificationPushService
{
    /// <summary>
    /// Gửi thông báo mới tới một user cụ thể (theo UserId).
    /// </summary>
    Task PushNewNotificationAsync(Guid userId, object payload, CancellationToken cancellationToken = default);
}
