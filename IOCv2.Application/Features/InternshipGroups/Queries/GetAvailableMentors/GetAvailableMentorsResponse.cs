namespace IOCv2.Application.Features.InternshipGroups.Queries.GetAvailableMentors;

public class AvailableMentorDto
{
    public Guid UserId { get; set; }
    public Guid EnterpriseUserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Position { get; set; }
    public int CurrentGroupCount { get; set; }
    public bool IsCurrentMentor { get; set; }
}
