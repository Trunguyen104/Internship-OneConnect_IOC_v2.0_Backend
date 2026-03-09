using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources
{
    public class GetAllProjectResourcesResponse : IMapFrom<Domain.Entities.ProjectResources>
    {
        public Guid ProjectResourceId { get; set; }
        public Guid ProjectId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public FileType ResourceType { get; set; }
        public string ResourceUrl { get; set; } = string.Empty;
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Domain.Entities.ProjectResources, GetAllProjectResourcesResponse>();
        }
    }
}
