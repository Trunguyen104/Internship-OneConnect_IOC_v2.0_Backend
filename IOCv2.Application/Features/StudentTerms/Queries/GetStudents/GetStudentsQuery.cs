using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.StudentTerms.Queries.GetStudents;

public record GetStudentsQuery : IRequest<Result<PaginatedResult<GetStudentsResponse>>>
{
    public Guid TermId { get; init; }
    public string? SearchTerm { get; init; }
    public PlacementStatus? PlacementStatus { get; init; }
    public EnrollmentStatus? EnrollmentStatus { get; init; }
    public string? Major { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
}
