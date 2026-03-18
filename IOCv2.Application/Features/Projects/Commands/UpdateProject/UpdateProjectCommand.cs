using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using System;

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    /// <summary>
    /// Command to update an existing project's information and status.
    /// </summary>
    public record UpdateProjectCommand : IRequest<Result<UpdateProjectResponse>>, IMapFrom<Project>
    {
        /// <summary>
        /// Identity of the project to be updated.
        /// </summary>
        [System.Text.Json.Serialization.JsonIgnore]
        public Guid ProjectId { get; set; }

        /// <summary>
        /// Optional new identity of the internship group (in case of transfer).
        /// </summary>
        public Guid? InternshipId { get; set; }

        /// <summary>
        /// New name for the project.
        /// </summary>
        public string? ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Updated description of the project.
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Updated start date.
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// Updated end date.
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// New lifecycle status of the project.
        /// </summary>
        public ProjectStatus? Status { get; set; }
    }
}
