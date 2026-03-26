using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Queries.GetSelfApplyApplications;

public record GetSelfApplyApplicationsQuery : IRequest<Result<PaginatedResult<GetSelfApplyApplicationsResponse>>>
{
    public string? SearchTerm { get; init; }

    /// <summary>Filter by specific status values (comma-separated or repeated). If null, defaults to active stages only.</summary>
    public string? Status { get; init; }

    public Guid? UniversityId { get; init; }

    /// <summary>Format: yyyy-MM (e.g. 2025-08). Filters by applied month.</summary>
    public string? MonthYear { get; set; }

    public string? JobTitle { get; set; }

    public Guid? JobId { get; set; }

    /// <summary>When true, includes terminal states (Placed, Rejected, Withdrawn). Default: false.</summary>
    public bool IncludeTerminal { get; init; } = false;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortColumn { get; init; }
    public string? SortOrder { get; init; }
}
