using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Stakeholders.DTOs;
using MediatR;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders
{
    public record GetStakeholdersQuery : IRequest<Result<PaginatedResult<StakeholderDto>>>
    {
        public Guid ProjectId { get; init; }
        public string? SearchTerm { get; init; }
        public string? SortColumn { get; init; }
        public string? SortOrder { get; init; } // "asc" or "desc"
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
    }
}
