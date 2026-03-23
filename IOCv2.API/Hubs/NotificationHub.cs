using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace IOCv2.API.Hubs;

/// <summary>
/// SignalR Hub cho Notification real-time.
/// Mỗi User connect sẽ được thêm vào group riêng "user-{UserId}",
/// cho phép push tới đúng user mà không broadcast toàn bộ.
/// </summary>
[Authorize]
public class NotificationHub : Hub
{
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(ILogger<NotificationHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        // UserId từ JWT claim NameIdentifier (set bởi UserIdProvider mặc định)
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(userId))
        {
            var groupName = $"user-{userId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} connected to NotificationHub. ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;

        if (!string.IsNullOrEmpty(userId))
        {
            var groupName = $"user-{userId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} disconnected from NotificationHub. ConnectionId: {ConnectionId}",
                userId, Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
