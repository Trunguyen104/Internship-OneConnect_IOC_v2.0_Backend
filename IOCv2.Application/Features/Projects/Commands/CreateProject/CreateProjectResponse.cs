using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System.Collections.Generic;

namespace IOCv2.Application.Features.Projects.Commands.CreateProject
{
    public class CreateProjectResponse : IMapFrom<Project>
    {
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectCode { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string? Deliverables { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public VisibilityStatus VisibilityStatus { get; set; }
        public OperationalStatus OperationalStatus { get; set; }
        public Guid? MentorId { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Danh sách tài liệu đính kèm đã upload/link trong cùng request tạo project</summary>
        public List<ProjectResourcesDTO> ProjectResources { get; set; } = new();

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Project, CreateProjectResponse>()
                .ForMember(dest => dest.VisibilityStatus, opt => opt.MapFrom(src => src.VisibilityStatus))
                .ForMember(dest => dest.OperationalStatus, opt => opt.MapFrom(src => src.OperationalStatus))
                .ForMember(dest => dest.ProjectResources,
                           opt => opt.MapFrom(src => src.ProjectResources));
        }
    }
}
