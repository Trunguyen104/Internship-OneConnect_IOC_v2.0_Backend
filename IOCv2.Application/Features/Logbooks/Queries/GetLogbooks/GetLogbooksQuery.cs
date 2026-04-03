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
    /// Query to get logbooks grouped by week for an internship group.
    /// </summary>
    public record GetLogbooksQuery : IRequest<Result<GetLogbooksByWeekResponse>>
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
        /// Optional internship week filter as CSV (e.g. "1,2").
        /// </summary>
        public string? WeekFilter { get; init; }


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
