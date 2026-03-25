using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.InternshipGroups.Queries.GetInternshipGroups
{
    /// <summary>
    /// Query to retrieve a paginated list of internship groups with optional filtering.
    /// </summary>
    public record GetInternshipGroupsQuery : IRequest<Result<PaginatedResult<GetInternshipGroupsResponse>>>
    {
        public int PageNumber { get; init; } = 1;
        public int PageSize { get; init; } = 10;
        public string? SearchTerm { get; init; }
        public GroupStatus? Status { get; init; }
        public Guid? PhaseId { get; init; }
        public bool IncludeArchived { get; init; } = false;
        public Guid? EnterpriseId { get; init; }
    }
}
