using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues
{
    /// <summary>
    /// Query to get a paginated list of stakeholder issues with optional filters.
    /// </summary>
    public record GetStakeholderIssuesQuery : IRequest<IOCv2.Application.Common.Models.Result<IOCv2.Application.Common.Models.PaginatedResult<GetStakeholderIssuesResponse>>>
    {
        public Guid? InternshipId { get; init; }
        public Guid? StakeholderId { get; init; }
        public StakeholderIssueStatus? Status { get; init; }

        /// <summary>
        /// Pagination and sorting parameters.
        /// </summary>
        public PaginationParams Pagination { get; init; } = new PaginationParams();
    }
}
