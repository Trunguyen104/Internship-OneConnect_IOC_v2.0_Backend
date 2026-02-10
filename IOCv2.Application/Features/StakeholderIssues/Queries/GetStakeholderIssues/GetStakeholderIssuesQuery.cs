using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Features.StakeholderIssues.DTOs;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.StakeholderIssues.Queries.GetStakeholderIssues;

public record GetStakeholderIssuesQuery : IRequest<Result<PagedResult<StakeholderIssueDto>>>
{
    public Guid? ProjectId { get; init; }
    public StakeholderIssueStatus? Status { get; init; }
    public Guid? StakeholderId { get; init; }
    public PaginationParams Pagination { get; init; } = new();
}
