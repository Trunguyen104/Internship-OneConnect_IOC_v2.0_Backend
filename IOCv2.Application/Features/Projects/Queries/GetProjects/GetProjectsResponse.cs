using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using AutoMapper;

namespace IOCv2.Application.Features.Projects.Queries.GetProjects;

public class GetProjectsResponse : IMapFrom<Project>
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Field { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Tags { get; set; }
    public int ViewCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }

    // Thêm các thông tin cơ bản
    public string InternshipTerm { get; set; } = string.Empty;
    public string Enterprise { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string MentorName { get; set; } = string.Empty;
    public int TotalMembers { get; set; }

    public void Mapping(MappingProfile profile)
    {
        profile.CreateMap<Project, GetProjectsResponse>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.InternshipTerm, opt => opt.MapFrom(src => src.Internship.Term.Name))
            .ForMember(dest => dest.Enterprise, opt => opt.MapFrom(src => src.Internship.Job.Enterprise.Name))
            .ForMember(dest => dest.University, opt => opt.MapFrom(src =>
                src.Internship.Student.User.UniversityUser != null
                ? src.Internship.Student.User.UniversityUser.University.Name
                : string.Empty))
            .ForMember(dest => dest.MentorName, opt => opt.MapFrom(src => src.Mentor != null ? src.Mentor.User.FullName : string.Empty))
            .ForMember(dest => dest.TotalMembers, opt => opt.MapFrom(src => src.Members.Count));
    }
}
