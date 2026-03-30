using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Queries.GetUniAssignApplications;

public record GetUniAssignApplicationsQuery : IRequest<Result<PaginatedResult<GetUniAssignApplicationsResponse>>>
{
    public string? SearchTerm { get; init; }
    public string? Status { get; init; }
    public Guid? UniversityId { get; init; }
    public string? MonthYear { get; init; }

    /// <summary>Filter by Intern Phase.</summary>
    public Guid? InternPhaseId { get; init; }

    /// <summary>When true, includes Placed and Rejected. Default: only PendingAssignment.</summary>
    public bool IncludeTerminal { get; init; } = false;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortColumn { get; init; }
    public string? SortOrder { get; init; }
}
