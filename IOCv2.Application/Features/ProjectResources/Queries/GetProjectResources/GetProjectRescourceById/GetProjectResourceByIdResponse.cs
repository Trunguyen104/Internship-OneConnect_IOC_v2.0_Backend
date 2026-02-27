using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.ProjectResources.Commands.UploadProjectResource;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetProjectRescourceById
{
    public class GetProjectResourceByIdResponse : IMapFrom<Domain.Entities.ProjectResources>
    {
        public Guid ProjectResourceId { get; set; }
        public Guid ProjectId { get; set; }
        public string ResourceName { get; set; }
        public FileType ResourceType { get; set; }
        public string ResourceUrl { get; set; }
        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Domain.Entities.ProjectResources, GetProjectResourceByIdResponse>()
                .ForMember(dest => dest.ProjectResourceId,
                    opt => opt.MapFrom(src => src.ProjectResourceId))
                .ForMember(dest => dest.ProjectId,
                    opt => opt.MapFrom(src => src.ProjectId))
                .ForMember(dest => dest.ResourceName,
                    opt => opt.MapFrom(src => src.ResourceName))
                .ForMember(dest => dest.ResourceType,
                    opt => opt.MapFrom(src => src.ResourceType))
                .ForMember(dest => dest.ResourceUrl,
                    opt => opt.MapFrom(src => src.ResourceUrl));
        }
    }
}
