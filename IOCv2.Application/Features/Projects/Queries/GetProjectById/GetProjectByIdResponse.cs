using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Projects.Queries.GetAProjects;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById
{
    public class GetProjectByIdResponse : IMapFrom<Project>
    {
        public Guid ProjectId { get; set; }
        public Guid InternshipId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectStatus? Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? UpdatedBy { get; set; }
        public DateTime? DeletedAt { get; set; }
        public List<ProjectResourcesDTO> ProjectResources { get; set; } = new();
        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Domain.Entities.ProjectResources, ProjectResourcesDTO>();
            profile.CreateMap<Project, GetProjectByIdResponse>()
                .ForMember(dest => dest.ProjectResources,
                           opt => opt.MapFrom(src => src.ProjectResources));
        }
    }
}
