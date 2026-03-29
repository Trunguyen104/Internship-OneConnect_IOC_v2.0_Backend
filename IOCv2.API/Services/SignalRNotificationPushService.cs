using IOCv2.Application.Interfaces;
using IOCv2.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace IOCv2.API.Services;

/// <summary>
/// Implement INotificationPushService dùng SignalR.
/// Đặt ở API layer vì cần reference NotificationHub — API project đã depend vào Application.
/// Được đăng ký ở Program.cs (không phải Infrastructure DI).
/// </summary>
public class SignalRNotificationPushService : INotificationPushService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationPushService> _logger;

    public SignalRNotificationPushService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationPushService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task PushNewNotificationAsync(Guid userId, object payload, CancellationToken cancellationToken = default)
    {
        var groupName = $"user-{userId}";

        await _hubContext.Clients.Group(groupName)
            .SendAsync("ReceiveNewNotification", payload, cancellationToken);

        _logger.LogDebug("Pushed ReceiveNewNotification to group {Group}", groupName);
    }
}
