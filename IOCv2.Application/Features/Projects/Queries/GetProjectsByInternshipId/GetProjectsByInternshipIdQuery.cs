using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByInternshipId
{
    /// <summary>
    /// Query to retrieve a paginated list of projects belonging to a specific internship group.
    /// </summary>
    public record GetProjectsByInternshipIdQuery : IRequest<Result<PaginatedResult<GetProjectsByInternshipIdResponse>>>
    {
        /// <summary>
        /// Identity of the target internship group.
        /// </summary>
        public Guid InternshipId { get; set; }

        /// <summary>
        /// Search text for project name and description.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by visibility status (Draft/Published).
        /// </summary>
        public VisibilityStatus? VisibilityStatus { get; init; }

        /// <summary>
        /// Filter by operational status (Unstarted/Active/Completed/Archived).
        /// </summary>
        public OperationalStatus? OperationalStatus { get; init; }

        /// <summary>
        /// Lower bound for project start date filter.
        /// </summary>
        public DateTime? FromDate { get; init; }

        /// <summary>
        /// Upper bound for project end date filter.
        /// </summary>
        public DateTime? ToDate { get; init; }

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
        public string? SortColumn { get; set; }

        /// <summary>
        /// Ordering direction ('asc' or 'desc').
        /// </summary>
        public string? SortOrder { get; set; }
    }
}
