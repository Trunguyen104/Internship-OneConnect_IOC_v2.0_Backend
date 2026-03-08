using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprints;

/// <summary>
/// Query to get all sprints for a project.
/// </summary>
public record GetSprintsQuery(
    Guid ProjectId,
    SprintStatus? StatusFilter,
    PaginationParams Pagination
) : IRequest<Result<PaginatedResult<GetSprintsResponse>>>;
