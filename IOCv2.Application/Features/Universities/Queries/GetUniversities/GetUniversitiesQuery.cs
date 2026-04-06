using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Universities.Queries.GetUniversities;

/// <summary>
/// Query to retrieve a paginated list of universities.
/// </summary>
public record GetUniversitiesQuery : IRequest<Result<PaginatedResult<GetUniversitiesResponse>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public UniversityStatus? Status { get; init; }
}
