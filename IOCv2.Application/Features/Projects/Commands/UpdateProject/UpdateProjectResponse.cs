using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    public class UpdateProjectResponse : IMapFrom<Project>
    {
        public Guid ProjectId { get; set; }
        public Guid? InternshipId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string? Deliverables { get; set; }
        public ProjectTemplate Template { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectStatus? Status { get; set; }
        public Guid? MentorId { get; set; }

        /// <summary>
        /// > 0 nghĩa là dự án đang Published và có sinh viên được gán — FE hiển thị warning.
        /// </summary>
        public int AssignedStudentCount { get; set; }

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Project, UpdateProjectResponse>()
                .ForMember(dest => dest.AssignedStudentCount, opt => opt.Ignore());
        }
    }
}
