using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Projects.Queries.GetAProjects;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById
{
    /// <summary>
    /// Detailed response model for a specific project.
    /// </summary>
    public class GetProjectByIdResponse : IMapFrom<Project>
    {
        /// <summary>
        /// Unique identifier for the project.
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Identity of the internship group associated with this project.
        /// </summary>
        public Guid InternshipId { get; set; }

        /// <summary>
        /// Name of the project.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Detailed description of the project goal and scope.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Planned or actual start date of the project.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Planned or actual end date of the project.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Current lifecycle status of the project.
        /// </summary>
        public ProjectStatus? Status { get; set; }

        /// <summary>
        /// Date and time when the project record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// List of resources (documents, links) associated with this project.
        /// </summary>
        public List<ProjectResourcesDTO> ProjectResources { get; set; } = new();

        /// <summary>
        /// Configures the mapping between the Project entity and its resources to this response DTO.
        /// </summary>
        /// <param name="profile">The Automapper profile.</param>
        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Domain.Entities.ProjectResources, ProjectResourcesDTO>();
            profile.CreateMap<Project, GetProjectByIdResponse>()
                .ForMember(dest => dest.ProjectResources,
                           opt => opt.MapFrom(src => src.ProjectResources));
        }
    }
}
