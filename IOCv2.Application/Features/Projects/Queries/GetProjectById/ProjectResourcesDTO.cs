using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById
{
    public class ProjectResourcesDTO : IMapFrom<Domain.Entities.ProjectResources>
    {
        public Guid ProjectId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string ResourceUrl { get; set; } = string.Empty;

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Domain.Entities.ProjectResources, ProjectResourcesDTO>()
                .ForMember(dest => dest.ResourceType, opt => opt.MapFrom(src => src.ResourceType.ToString()));
        }
    }
}
