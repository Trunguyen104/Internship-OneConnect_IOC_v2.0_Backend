namespace IOCv2.Domain.Enums;

public enum MentorActionType
{
    Assign = 1, // Gán lần đầu (oldMentorId = null)
    Change = 2  // Đổi mentor (oldMentorId != null)
}
