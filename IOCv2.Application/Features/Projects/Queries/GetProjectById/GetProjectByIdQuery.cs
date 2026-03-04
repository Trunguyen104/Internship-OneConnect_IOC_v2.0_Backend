using IOCv2.Application.Common.Models;
using MediatR;
using System;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectById
{
    /// <summary>
    /// Query to retrieve full details of a single project by its identity.
    /// </summary>
    public record GetProjectByIdQuery : IRequest<Result<GetProjectByIdResponse>>
    {
        /// <summary>
        /// Unique identifier for the requested project.
        /// </summary>
        public Guid ProjectId { get; init; }
    }
}
