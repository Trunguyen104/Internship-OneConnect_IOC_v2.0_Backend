namespace IOCv2.Application.Features.InternshipGroups.Commands.AssignMentorToGroup;

public class AssignMentorToGroupResponse
{
    public Guid InternshipGroupId { get; set; }
    public Guid? MentorEnterpriseUserId { get; set; }
    public Guid MentorUserId { get; set; }
    public string MentorFullName { get; set; } = string.Empty;
    public string MentorEmail { get; set; } = string.Empty;
    public string ActionType { get; set; } = string.Empty;     // "Assign" | "Change"
    public DateTime UpdatedAt { get; set; }
}
