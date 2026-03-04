using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Queries.GetSprints;

/// <summary>
/// Query to get all sprints for a project.
/// </summary>
/// <param name="ProjectId">The ID of the project.</param>
/// <param name="StatusFilter">Optional status filter ("Planned", "Active", "Completed").</param>
/// <param name="Pagination">Pagination and search parameters.</param>
public record GetSprintsQuery(
    Guid ProjectId,
    string? StatusFilter,
    PaginationParams Pagination
) : IRequest<Result<PaginatedResult<GetSprintsResponse>>>;
