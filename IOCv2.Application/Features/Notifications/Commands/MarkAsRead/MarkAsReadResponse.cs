namespace IOCv2.Application.Features.Notifications.Commands.MarkAsRead;

public class MarkAsReadResponse
{
    public Guid NotificationId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
}
