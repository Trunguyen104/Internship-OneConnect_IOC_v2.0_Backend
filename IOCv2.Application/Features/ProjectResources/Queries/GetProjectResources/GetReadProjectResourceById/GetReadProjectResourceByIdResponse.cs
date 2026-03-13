using AutoMapper;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetReadProjectResourceById
{
    public class GetReadProjectResourceByIdResponse : IMapFrom<Domain.Entities.ProjectResources>
    {
        public Guid ProjectResourceId { get; set; }
        public Guid ProjectId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public FileType ResourceType { get; set; }
        public string ResourceUrl { get; set; } = string.Empty;
        public void Mapping(Profile profile)
        {
            profile.CreateMap<Domain.Entities.ProjectResources, GetReadProjectResourceByIdResponse>();
        }
    }
}