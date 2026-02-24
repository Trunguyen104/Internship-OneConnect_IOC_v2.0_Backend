using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using AutoMapper;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById;

// DTO con dành cho Member của dự án
public class ProjectMemberDto : IMapFrom<ProjectMember>
{
    public int No { get; set; } // Số thứ tự theo sắp xếp
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    public void Mapping(MappingProfile profile)
    {
        profile.CreateMap<ProjectMember, ProjectMemberDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Student.User.FullName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Student.User.Email))
            .ForMember(dest => dest.AvatarUrl, opt => opt.MapFrom(src => src.Student.User.AvatarUrl))
            .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.Student.User.DateOfBirth))
            .ForMember(dest => dest.Gender, opt => opt.MapFrom(src => src.Student.User.Gender.ToString()))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.ToString()));
    }
}

// DTO chính trả về cho chi tiết dự án
public class GetProjectByIdResponse : IMapFrom<Project>
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string InternshipTerm { get; set; } = string.Empty;
    public string Enterprise { get; set; } = string.Empty;
    public string University { get; set; } = string.Empty;
    public string MentorName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ProjectMemberDto> Members { get; set; } = new();

    public void Mapping(MappingProfile profile)
    {
        profile.CreateMap<Project, GetProjectByIdResponse>()
            .ForMember(dest => dest.InternshipTerm, opt => opt.MapFrom(src => src.Internship.Term.Name))
            .ForMember(dest => dest.Enterprise, opt => opt.MapFrom(src => src.Internship.Job.Enterprise.Name))
            .ForMember(dest => dest.University, opt => opt.MapFrom(src =>
                src.Internship.Student.User.UniversityUser != null
                ? src.Internship.Student.User.UniversityUser.University.Name
                : string.Empty))
            .ForMember(dest => dest.MentorName, opt => opt.MapFrom(src => src.Mentor != null ? src.Mentor.User.FullName : string.Empty))
            .ForMember(dest => dest.Members, opt => opt.Ignore()); // Member sẽ được xử lý riêng để đánh số thứ tự (No)
    }
}
