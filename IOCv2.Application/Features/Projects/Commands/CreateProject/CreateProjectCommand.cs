using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using MediatR;
using System;

namespace IOCv2.Application.Features.Projects.Commands.CreateProject
{
    /// <summary>
    /// Command to create a new project within an internship group.
    /// </summary>
    public record CreateProjectCommand : IRequest<Result<CreateProjectResponse>>, IMapFrom<Project>
    {
        /// <summary>
        /// Identity of the internship group that will host this project.
        /// </summary>
        public Guid InternshipId { get; set; }

        /// <summary>
        /// Human-readable name of the project. Must be unique within the internship.
        /// </summary>
        public string ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Optional description of the project scope and objectives.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Estimated or confirmed start date of the project.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Target completion date of the project.
        /// </summary>
        public DateTime? EndDate { get; set; }
    }
}
