using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System;

namespace IOCv2.Application.Features.Projects.Queries.GetAllProjects
{
    /// <summary>
    /// Query to retrieve a paginated list of all projects with filtering and search capabilities.
    /// </summary>
    public record GetAllProjectsQuery : IRequest<Result<PaginatedResult<GetAllProjectsResponse>>>
    {
        /// <summary>
        /// Text input to search for matches in project name and description.
        /// </summary>
        public string? SearchTerm { get; set; }

        /// <summary>
        /// Filter by visibility status (Draft or Published).
        /// </summary>
        public VisibilityStatus? VisibilityStatus { get; set; }

        /// <summary>
        /// Filter by operational status (Unstarted, Active, Completed, Archived).
        /// </summary>
        public OperationalStatus? OperationalStatus { get; set; }

        /// <summary>Show archived projects (default: false - archived hidden)</summary>
        public bool ShowArchived { get; set; } = false;

        /// <summary>
        /// Filter records starting from this date.
        /// </summary>
        public DateTime? FromDate { get; set; }

        /// <summary>
        /// Filter records ending at this date.
        /// </summary>
        public DateTime? ToDate { get; init; }

        /// <summary>
        /// Filter by the associated internship group.
        /// </summary>
        public Guid? InternshipId { get; init; }

        /// <summary>
        /// Filter by a specific student associated with the project.
        /// </summary>
        public Guid? StudentId { get; init; }

        /// <summary>
        /// Current page index (starts at 1).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of records per page.
        /// </summary>
        public int PageSize { get; init; } = 10;

        /// <summary>
        /// Column name to sort the results by.
        /// </summary>
        public string? SortColumn { get; set; }

        /// <summary>
        /// Sorting direction ('asc' or 'desc').
        /// </summary>
        public string? SortOrder { get; set; }

        /// <summary>F3 (AC-01): Filter theo lĩnh vực dự án (VD: CNTT, Mobile, IoT)</summary>
        public string? Field { get; set; }
    }
}
