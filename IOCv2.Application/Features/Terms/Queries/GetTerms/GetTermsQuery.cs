using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Terms.Queries.GetTerms;

public record GetTermsQuery : IRequest<Result<PaginatedResult<GetTermsResponse>>>
{
    public string? SearchTerm { get; init; }
    public TermDisplayStatus? Status { get; init; }
    public int? Year { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortColumn { get; init; }
    public string? SortOrder { get; init; }

    /// <summary>
    ///     Only used by SuperAdmin to filter terms by a specific university.
    ///     If null, SuperAdmin retrieves terms across all universities.
    ///     SchoolAdmin does not need to provide this — it is resolved automatically.
    /// </summary>
    public Guid? UniversityId { get; init; }
}