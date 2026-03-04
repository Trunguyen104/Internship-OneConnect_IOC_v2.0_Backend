using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders
{
    /// <summary>
    /// Query to get a paginated list of stakeholders for a project.
    /// </summary>
    public record GetStakeholdersQuery : IRequest<Result<PaginatedResult<GetStakeholdersResponse>>>
    {
        /// <summary>
        /// The ID of the project to get stakeholders for.
        /// </summary>
        public Guid ProjectId { get; init; }

        /// <summary>
        /// Optional search term to filter stakeholders by Name, Email, or Role.
        /// </summary>
        public string? SearchTerm { get; init; }

        /// <summary>
        /// Column to sort by.
        /// </summary>
        public string? SortColumn { get; init; }

        /// <summary>
        /// Sort order ("asc" or "desc").
        /// </summary>
        public string? SortOrder { get; init; }

        /// <summary>
        /// Page number (starting from 1).
        /// </summary>
        public int PageNumber { get; init; } = 1;

        /// <summary>
        /// Number of items per page.
        /// </summary>
        public int PageSize { get; init; } = 10;
    }
}
