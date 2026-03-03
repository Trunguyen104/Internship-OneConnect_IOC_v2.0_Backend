using IOCv2.Application.Common.Models;
using MediatR;
using System;

namespace IOCv2.Application.Features.Projects.Commands.DeleteProject
{
    /// <summary>
    /// Command to remove a project record (typically via soft delete).
    /// </summary>
    public record DeleteProjectCommand : IRequest<Result<string>>
    {
        /// <summary>
        /// Identity of the project to be deleted.
        /// </summary>
        public Guid ProjectId { get; set; }
    }
}
