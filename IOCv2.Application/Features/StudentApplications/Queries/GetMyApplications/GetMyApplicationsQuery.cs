using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.StudentApplications.Queries.GetMyApplications;

public record GetMyApplicationsQuery : IRequest<Result<PaginatedResult<GetMyApplicationsResponse>>>
{
    /// <summary>Filter by Status. Null = default (active only).</summary>
    public string? Status { get; init; }

    /// <summary>When true, include Placed, Rejected, Withdrawn records.</summary>
    public bool IncludeTerminal { get; init; } = false;

    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
}
