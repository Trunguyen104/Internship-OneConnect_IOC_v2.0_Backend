using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid Id { get; set; }

    // Notifications can be targeted at specific users
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Type { get; set; }

    public bool IsRead { get; set; } = false;

    // Add reference to related entity if needed (e.g. TermId, InternshipGroupId)
    public Guid? RelatedId { get; set; }
}
