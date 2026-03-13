using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetMyInternshipGroups;

public class GetMyInternshipGroupsResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? EnterpriseId { get; set; }
    public Guid SchoolId { get; set; }
    public Guid TermId { get; set; }
    public InternshipStatus GroupStatus { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool AllowLecturerView { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public MineEnterpriseDto? Enterprise { get; set; }
    public MineSchoolDto? School { get; set; }
    public MineTermDto? Term { get; set; }
    public MineProjectDto? Project { get; set; }
    public List<MineMentorDto> Mentors { get; set; } = new();
    public int StudentCount { get; set; }
    public int EvaluationCount { get; set; }
    public Guid? ProjectId { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Guid? CreatedBy { get; set; }
    public Guid? UpdatedBy { get; set; }

    public static GetMyInternshipGroupsResponse FromEntity(InternshipGroup group, Project? project)
    {
        return new GetMyInternshipGroupsResponse
        {
            Id = group.InternshipId,
            Name = group.GroupName,
            EnterpriseId = group.EnterpriseId,
            SchoolId = group.Term.UniversityId,
            TermId = group.TermId,
            GroupStatus = group.Status,
            StartDate = group.StartDate,
            EndDate = group.EndDate,
            Description = string.Empty,
            AllowLecturerView = false,
            CreatedAt = group.CreatedAt,
            UpdatedAt = group.UpdatedAt,
            Enterprise = group.Enterprise == null
                ? null
                : new MineEnterpriseDto
                {
                    Id = group.Enterprise.EnterpriseId,
                    Name = group.Enterprise.Name
                },
            School = group.Term.University == null
                ? null
                : new MineSchoolDto
                {
                    Id = group.Term.University.UniversityId,
                    Name = group.Term.University.Name
                },
            Term = new MineTermDto
            {
                Id = group.Term.TermId,
                Name = group.Term.Name
            },
            Project = project == null
                ? null
                : new MineProjectDto
                {
                    Id = project.ProjectId,
                    Name = project.ProjectName,
                    Domain = project.InternshipId,
                    SpaceTemplate = null
                },
            Mentors = group.Mentor == null || group.Mentor.User == null
                ? new List<MineMentorDto>()
                : new List<MineMentorDto>
                {
                    new()
                    {
                        Id = group.Mentor.EnterpriseUserId,
                        FullName = group.Mentor.User.FullName,
                        Email = group.Mentor.User.Email
                    }
                },
            StudentCount = group.Members.Count,
            EvaluationCount = 0, // Will be populated in the handler for efficiency if needed, or here if we have it
            ProjectId = project?.ProjectId,
            DeletedAt = group.DeletedAt,
            CreatedBy = group.CreatedBy,
            UpdatedBy = group.UpdatedBy
        };
    }
}

public class MineEnterpriseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MineSchoolDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MineTermDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class MineProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? Domain { get; set; }
    public MineSpaceTemplateDto? SpaceTemplate { get; set; }
}

public class MineSpaceTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BoardType { get; set; } = string.Empty;
}

public class MineMentorDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
