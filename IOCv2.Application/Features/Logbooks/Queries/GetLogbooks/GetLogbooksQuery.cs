using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Admin.Users.Queries.GetAdminUsers;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbooks
{
    /// <summary>
    /// Query to get paginated logbooks for a project.
    /// </summary>
    public record GetLogbooksQuery : IRequest<Result<PaginatedResult<GetLogbooksResponse>>>
    {
        /// <summary>
        /// Project ID from route.
        /// </summary>
        public Guid ProjectId { get; init; }

        /// <summary>
        /// Optional status filter.
        /// </summary>
        public string? Status { get; init; }

        /// <summary>
        /// Page number (default 1).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Items per page (default 10).
        /// </summary>
        public int PageSize { get; init; } = 10;

        /// <summary>
        /// Column to sort by.
        /// </summary>
        public string? SortColumn { get; init; }

        /// <summary>
        /// Sort direction (asc/desc).
        /// </summary>
        public string? SortOrder { get; init; } // "asc" or "desc"
    }
}
