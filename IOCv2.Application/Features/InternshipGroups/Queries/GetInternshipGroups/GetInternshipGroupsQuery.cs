using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups
{
    /// <summary>
    /// Query to retrieve a paginated list of internship groups with optional filtering.
    /// </summary>
    public record GetInternshipGroupsQuery : IRequest<Result<PaginatedResult<GetInternshipGroupsResponse>>>
    {
        /// <summary>
        /// Page number to retrieve (1-indexed).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; init; } = 10;

        /// <summary>
        /// Optional string to search in group name or enterprise name.
        /// </summary>
        public string? SearchTerm { get; init; }

        /// <summary>
        /// Optional status filter.
        /// </summary>
        public short? Status { get; init; }
    }
}
