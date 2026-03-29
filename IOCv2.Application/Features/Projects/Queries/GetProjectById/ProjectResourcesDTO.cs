using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Enums;
using System;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById
{
    public class ProjectResourcesDTO : IMapFrom<Domain.Entities.ProjectResources>
    {
        public Guid ProjectResourceId { get; set; }
        public Guid ProjectId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public FileType ResourceType { get; set; }


        public string ResourceUrl { get; set; } = string.Empty;

        public void Mapping(Profile profile)
        {
            profile.CreateMap<Domain.Entities.ProjectResources, ProjectResourcesDTO>()
                .ForMember(dest => dest.ProjectResourceId, opt => opt.MapFrom(src => src.ProjectResourceId))
                .ForMember(dest => dest.ResourceType, opt => opt.MapFrom(src => src.ResourceType));

        }
    }
}
