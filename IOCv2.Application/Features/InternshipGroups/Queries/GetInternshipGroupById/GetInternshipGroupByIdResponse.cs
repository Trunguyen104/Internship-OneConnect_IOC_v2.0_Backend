using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroupById
{
    /// <summary>
    /// Detailed response for a specific internship group.
    /// </summary>
    public class GetInternshipGroupByIdResponse : IMapFrom<InternshipGroup>
    {
        public Guid InternshipId { get; set; }
        public Guid TermId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public Guid? EnterpriseId { get; set; }
        public string? EnterpriseName { get; set; }
        public Guid? MentorId { get; set; }
        public string? MentorName { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public InternshipStatus Status { get; set; }

        /// <summary>
        /// List of students currently in the group.
        /// </summary>
        public List<InternshipStudentDto> Members { get; set; } = new List<InternshipStudentDto>();

        public void Mapping(Profile profile)
        {
            profile.CreateMap<InternshipGroup, GetInternshipGroupByIdResponse>()
                .ForMember(d => d.EnterpriseName, opt => opt.MapFrom(s => s.Enterprise != null ? s.Enterprise.Name : null))
                .ForMember(d => d.MentorName, opt => opt.MapFrom(s => s.Mentor != null && s.Mentor.User != null ? s.Mentor.User.FullName : null))
                .ForMember(d => d.Members, opt => opt.MapFrom(s => s.Members))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status));
        }
    }

    /// <summary>
    /// Detailed DTO for a student within an internship group.
    /// </summary>
    public class InternshipStudentDto : IMapFrom<InternshipStudent>
    {
        public Guid StudentId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? StudentCode { get; set; }
        public string? UniversityName { get; set; }
        public InternshipRole Role { get; set; }
        public InternshipStatus Status { get; set; }
        public DateTime JoinedAt { get; set; }

        public void Mapping(Profile profile)
        {
            profile.CreateMap<InternshipStudent, InternshipStudentDto>()
                .ForMember(d => d.FullName, opt => opt.MapFrom(s => s.Student != null && s.Student.User != null ? s.Student.User.FullName : null))
                .ForMember(d => d.Email, opt => opt.MapFrom(s => s.Student != null && s.Student.User != null ? s.Student.User.Email : null))
                .ForMember(d => d.StudentCode, opt => opt.MapFrom(s => s.Student != null && s.Student.User != null ? s.Student.User.UserCode : null))
                .ForMember(d => d.UniversityName, opt => opt.MapFrom(s => s.Student != null && s.Student.User != null && s.Student.User.UniversityUser != null && s.Student.User.UniversityUser.University != null ? s.Student.User.UniversityUser.University.Name : null))
                .ForMember(d => d.Role, opt => opt.MapFrom(s => s.Role))
                .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status));
        }
    }
}
