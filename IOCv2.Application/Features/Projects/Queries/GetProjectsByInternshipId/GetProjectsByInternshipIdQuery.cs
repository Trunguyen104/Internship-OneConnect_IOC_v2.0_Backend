using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectsByInternshipId
{
    /// <summary>
    /// Query to retrieve a paginated list of projects belonging to a specific internship group.
    /// </summary>
    public class GetProjectsByInternshipIdQuery : IRequest<Result<PaginatedResult<GetProjectsByInternshipIdResponse>>>
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
        /// Filter by project lifecycle status.
        /// </summary>
        public ProjectStatus? Status { get; set; }

        /// <summary>
        /// Lower bound for project start date filter.
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Upper bound for project end date filter.
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Page number for pagination.
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of items per result set.
        /// </summary>
        public int PageSize { get; set; } = 10;

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
