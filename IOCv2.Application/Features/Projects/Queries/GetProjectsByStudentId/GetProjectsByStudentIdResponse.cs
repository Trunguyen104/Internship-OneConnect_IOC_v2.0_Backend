using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId
{
    /// <summary>
    /// Response model for projects associated with a specific student.
    /// </summary>
    public class GetProjectsByStudentIdResponse : IMapFrom<Project>
    {
        /// <summary>
        /// Unique identifier for the project.
        /// </summary>
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Identity of the internship group associated with this project.
        /// </summary>
        public Guid? InternshipId { get; set; }

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
        /// Visibility status of the project.
        /// </summary>
        public VisibilityStatus VisibilityStatus { get; set; }

        /// <summary>
        /// Operational status of the project.
        /// </summary>
        public OperationalStatus OperationalStatus { get; set; }

        /// <summary>
        /// Workflow template used by the project.
        /// </summary>
        public ProjectTemplate Template { get; set; }

        /// <summary>
        /// Date and time when the project record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// List of resources associated with the project.
        /// </summary>
        public List<ProjectResourcesDTO> ProjectResources { get; set; } = new();

        /// <summary>
        /// Name of the internship group this project belongs to.
        /// </summary>
        public string? GroupName { get; set; }

        /// <summary>
        /// Full name of the mentor managing this project.
        /// </summary>
        public string? MentorName { get; set; }

        /// <summary>
        /// Configures the mapping between Project entity and this response DTO.
        /// </summary>
        /// <param name="profile">The Automapper profile.</param>
        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Domain.Entities.ProjectResources, ProjectResourcesDTO>();

            profile.CreateMap<Project, GetProjectsByStudentIdResponse>()
                   .ForMember(dest => dest.ProjectResources,
                               opt => opt.MapFrom(src => src.ProjectResources))
                   .ForMember(dest => dest.GroupName,
                               opt => opt.MapFrom(src => src.InternshipGroup != null ? src.InternshipGroup.GroupName : null))
                   .ForMember(dest => dest.MentorName,
                               opt => opt.MapFrom(src => src.InternshipGroup != null && src.InternshipGroup.Mentor != null
                                   ? src.InternshipGroup.Mentor.User.FullName
                                   : null));
        }

    }
}
