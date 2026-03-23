using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public NotificationType Type { get; set; }

    /// <summary>
    /// Đa hình — tên entity liên quan, ví dụ: "InternshipApplication", "Evaluation"
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// ID của entity liên quan (nullable — có thể là thông báo không gắn entity cụ thể)
    /// </summary>
    public Guid? ReferenceId { get; set; }

    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;
}
