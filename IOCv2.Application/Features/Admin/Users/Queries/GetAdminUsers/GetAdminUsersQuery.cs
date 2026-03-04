using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Admin.Users.Queries.GetAdminUsers
{
    /// <summary>
    /// Query to get a paginated list of administrative users.
    /// </summary>
    public record GetAdminUsersQuery : IRequest<Result<PaginatedResult<GetAdminUsersResponse>>>
    {
        /// <summary>
        /// (Optional) Search term for name, email, or code.
        /// </summary>
        public string? SearchTerm { get; init; }

        /// <summary>
        /// (Optional) Filter by role.
        /// </summary>
        public string? Role { get; init; }

        /// <summary>
        /// (Optional) Filter by status.
        /// </summary>
        public string? Status { get; init; }
        
        /// <summary>
        /// Page number (starting from 1).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; init; } = 10;
        
        /// <summary>
        /// (Optional) Column to sort by.
        /// </summary>
        public string? SortColumn { get; init; }

        /// <summary>
        /// (Optional) Sort order (asc/desc).
        /// </summary>
        public string? SortOrder { get; init; } // "asc" or "desc"
    }
}
