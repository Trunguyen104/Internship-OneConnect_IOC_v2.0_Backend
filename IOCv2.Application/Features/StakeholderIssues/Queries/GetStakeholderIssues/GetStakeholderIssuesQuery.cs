using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues
{
    public record GetStakeholderIssuesQuery : IRequest<Result<PagedResult<GetStakeholderIssuesResponse>>>
    {
        public Guid? ProjectId { get; init; }
        public string? Status { get; init; }
        public Guid? StakeholderId { get; init; }
        public PaginationParams Pagination { get; init; } = new();
    }
}

