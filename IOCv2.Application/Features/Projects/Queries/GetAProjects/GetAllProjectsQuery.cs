using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;

namespace IOCv2.Application.Features.Projects.Queries.GetAProjects
{
    /// <summary>
    /// Query to retrieve a paginated list of all projects with filtering and search capabilities.
    /// </summary>
    public class GetAllProjectsQuery : IRequest<Result<PaginatedResult<GetAllProjectsResponse>>>
    {
        /// <summary>
        /// Text input to search for matches in project name and description.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by specific project status.
        /// </summary>
        public ProjectStatus? Status { get; set; }

        /// <summary>
        /// Filter records starting from this date.
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter records ending at this date.
        /// </summary>
        public DateTime? ToDate { get; set; }

        /// <summary>
        /// Filter by the associated internship group.
        /// </summary>
        public Guid? InternshipId { get; set; }

        /// <summary>
        /// Filter by a specific student associated with the project.
        /// </summary>
        public Guid? StudentId { get; set; }

        /// <summary>
        /// Current page index (starts at 1).
        /// </summary>
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Number of records per page.
        /// </summary>
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// Column name to sort the results by.
        /// </summary>
        public string? SortColumn { get; set; }

        /// <summary>
        /// Sorting direction ('asc' or 'desc').
        /// </summary>
        public string? SortOrder { get; set; }
    }
}
