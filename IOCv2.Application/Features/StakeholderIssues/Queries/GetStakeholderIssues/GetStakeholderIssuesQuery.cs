using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues
{
    /// <summary>
    /// Query to get a paginated list of stakeholder issues with optional filters.
    /// </summary>
    public record GetStakeholderIssuesQuery : IRequest<IOCv2.Application.Common.Models.Result<IOCv2.Application.Common.Models.PaginatedResult<GetStakeholderIssuesResponse>>>
    {
        /// <summary>
        /// Optional project filter.
        /// </summary>
        public Guid? ProjectId { get; init; }

        /// <summary>
        /// Optional stakeholder filter.
        /// </summary>
        public Guid? StakeholderId { get; init; }

        /// <summary>
        /// Optional status filter (Open, InProgress, Resolved, Closed).
        /// </summary>
        public string? Status { get; init; }

        /// <summary>
        /// Pagination and sorting parameters.
        /// </summary>
        public PaginationParams Pagination { get; init; } = new PaginationParams();
    }
}
