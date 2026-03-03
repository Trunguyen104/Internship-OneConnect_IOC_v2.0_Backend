using IOCv2.Application.Extensions.Mappings;
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
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// List of resources associated with the project.
        /// </summary>
        public List<Domain.Entities.ProjectResources> ProjectResources { get; set; } = new();
    }
}
