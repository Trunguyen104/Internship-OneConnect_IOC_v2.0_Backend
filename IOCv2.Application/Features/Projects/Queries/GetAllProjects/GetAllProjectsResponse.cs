using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Projects.Queries.GetAllProjects
{
    /// <summary>
    /// Response model for a single project in a paginated list.
    /// </summary>
    public class GetAllProjectsResponse : IMapFrom<Project>
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

        /// <summary>Mã dự án</summary>
        public string ProjectCode { get; set; } = string.Empty;

        /// <summary>Lĩnh vực dự án</summary>
        public string Field { get; set; } = string.Empty;

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
        /// Visibility status of the project (Draft or Published).
        /// </summary>
        public VisibilityStatus VisibilityStatus { get; set; }

        /// <summary>
        /// Operational status of the project (Unstarted, Active, Completed, Archived).
        /// </summary>
        public OperationalStatus OperationalStatus { get; set; }

        /// <summary>Template dự án (None, Scrum, Kanban)</summary>
        public ProjectTemplate Template { get; set; }

        /// <summary>
        /// Date and time when the project record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Configures the mapping between the Project entity and this response DTO.
        /// </summary>
        /// <param name="profile">The Automapper profile.</param>
        public void Mapping(MappingProfile profile)
        {
            profile.CreateMap<Project, GetAllProjectsResponse>();
        }
    }
}
