using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Admin.UserManagement.Queries.GetUsers
{
    /// <summary>
    /// Query to get a paginated list of users with hierarchical filtering.
    /// </summary>
    public record GetUsersQuery : IRequest<Result<PaginatedResult<GetUsersResponse>>>
    {
        /// <summary>
        /// (Optional) Search term for name, email, or code.
        /// </summary>
        public string? SearchTerm { get; init; }

        /// <summary>
        /// (Optional) Filter by role.
        /// </summary>
        public UserRole? Role { get; init; }

        /// <summary>
        /// (Optional) Filter by status.
        /// </summary>
        public UserStatus? Status { get; init; }
        
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
