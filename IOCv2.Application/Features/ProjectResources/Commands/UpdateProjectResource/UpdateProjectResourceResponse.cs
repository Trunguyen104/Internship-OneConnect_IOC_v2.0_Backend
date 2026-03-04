using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource
{
    public class UpdateProjectResourceResponse : IMapFrom<Domain.Entities.ProjectResources>
    {
        public Guid ProjectResourceId { get; set; }
        public Guid? ProjectId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public string ResourceType { get; set; } = string.Empty;
        public string ResourceUrl { get; set; } = string.Empty;
        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Domain.Entities.ProjectResources, UpdateProjectResourceResponse>()
                .ForMember(dest => dest.ResourceType, opt => opt.MapFrom(src => src.ResourceType.ToString()));
        }
    }
}
