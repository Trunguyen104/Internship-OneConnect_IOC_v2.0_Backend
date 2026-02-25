using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprints;

public record GetSprintsQuery(
    Guid ProjectId,
    string? StatusFilter,   // FE truyền "Planned", "Active", "Completed" (hoặc null)
    PaginationParams Pagination
) : IRequest<Result<PagedResult<GetSprintsResponse>>>;
