using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Admin.UserManagement.Queries.GetUsers;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Logbooks.Queries.GetLogbooks
{
    /// <summary>
    /// Query to get paginated logbooks for an internship group.
    /// </summary>
    public record GetLogbooksQuery : IRequest<Result<PaginatedResult<GetLogbooksResponse>>>
    {
        /// <summary>
        /// Internship group ID from route.
        /// </summary>
        public Guid InternshipId { get; init; }

        /// <summary>
        /// Optional status filter.
        /// </summary>
        public LogbookStatus? Status { get; init; }

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
