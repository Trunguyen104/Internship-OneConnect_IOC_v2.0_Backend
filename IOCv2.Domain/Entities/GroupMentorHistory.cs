using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class GroupMentorHistory
{
    public Guid HistoryId { get; private set; }
    public Guid InternshipGroupId { get; private set; }
    public Guid? OldMentorId { get; private set; }  // EnterpriseUserId cũ, null nếu gán lần đầu
    public Guid? NewMentorId { get; private set; }  // EnterpriseUserId mới
    public Guid ActorId { get; private set; }       // UserId của HR thực hiện
    public MentorActionType ActionType { get; private set; }
    public DateTime Timestamp { get; private set; }

    // Navigation properties
    public virtual InternshipGroup InternshipGroup { get; set; } = null!;
    public virtual EnterpriseUser? OldMentor { get; set; }
    public virtual EnterpriseUser? NewMentor { get; set; }
    public virtual User Actor { get; set; } = null!;

    protected GroupMentorHistory() { }

    public static GroupMentorHistory Create(
        Guid internshipGroupId,
        Guid? oldMentorId,
        Guid? newMentorId,
        Guid actorId,
        MentorActionType actionType)
    {
        return new GroupMentorHistory
        {
            HistoryId = Guid.NewGuid(),
            InternshipGroupId = internshipGroupId,
            OldMentorId = oldMentorId,
            NewMentorId = newMentorId,
            ActorId = actorId,
            ActionType = actionType,
            Timestamp = DateTime.UtcNow
        };
    }
}
