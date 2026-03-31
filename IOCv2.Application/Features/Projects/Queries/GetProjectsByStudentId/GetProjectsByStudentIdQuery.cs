using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByStudentId
{
    /// <summary>
    /// Query to retrieve projects associated with a particular student.
    /// </summary>
    public record GetProjectsByStudentIdQuery : IRequest<Result<PaginatedResult<GetProjectsByStudentIdResponse>>>
    {
        /// <summary>
        /// Search text for project name and description.
        /// </summary>
        public string? SearchTerm { get; init; }

        /// <summary>
        /// Filter by visibility status (Draft/Published).
        /// </summary>
        public VisibilityStatus? VisibilityStatus { get; init; }

        /// <summary>
        /// Filter by operational status (Unstarted/Active/Completed/Archived).
        /// </summary>
        public OperationalStatus? OperationalStatus { get; init; }

        /// <summary>
        /// Page number for pagination.
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of items per result set.
        /// </summary>
        public int PageSize { get; init; } = 10;

        /// <summary>
        /// Property name to order by.
        /// </summary>
        public string? SortColumn { get; init; }

        /// <summary>
        /// Ordering direction ('asc' or 'desc').
        /// </summary>
        public string? SortOrder { get; init; }

    }
}
