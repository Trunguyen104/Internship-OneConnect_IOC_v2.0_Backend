using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByInternshipId
{
    /// <summary>
    /// Response model for projects filtered by internship group.
    /// </summary>
    public class GetProjectsByInternshipIdResponse : IMapFrom<Project>
    {
        public Guid ProjectId { get; set; }
        public Guid? InternshipId { get; set; }
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>Mã dự án</summary>
        public string ProjectCode { get; set; } = string.Empty;

        /// <summary>Lĩnh vực dự án</summary>
        public string Field { get; set; } = string.Empty;

        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public VisibilityStatus VisibilityStatus { get; set; }
        public OperationalStatus OperationalStatus { get; set; }
        public ProjectTemplate Template { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Tài liệu đính kèm dự án</summary>
        public List<ProjectResourcesDTO> ProjectResources { get; set; } = new();

        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Project, GetProjectsByInternshipIdResponse>()
                .ForMember(dest => dest.ProjectResources,
                           opt => opt.MapFrom(src => src.ProjectResources));
        }
    }
}
