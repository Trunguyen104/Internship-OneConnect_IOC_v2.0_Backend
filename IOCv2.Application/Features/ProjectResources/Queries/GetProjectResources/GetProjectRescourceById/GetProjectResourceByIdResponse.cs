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
        public string ResourceName { get; set; } = string.Empty;
        public FileType ResourceType { get; set; }


        public string ResourceUrl { get; set; } = string.Empty;
        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Domain.Entities.ProjectResources, GetProjectResourceByIdResponse>();

        }
    }
}
