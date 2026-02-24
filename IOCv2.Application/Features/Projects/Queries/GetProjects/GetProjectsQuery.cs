using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Projects.Queries.GetProjects;

public record GetProjectsQuery : IRequest<Result<PaginatedResult<GetProjectsResponse>>>
{
    public string? SearchTerm { get; init; }
    public string? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortColumn { get; init; }
    public string? SortOrder { get; init; }
}
