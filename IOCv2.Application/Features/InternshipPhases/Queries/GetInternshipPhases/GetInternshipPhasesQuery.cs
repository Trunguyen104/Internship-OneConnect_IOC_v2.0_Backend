using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhases;

public record GetInternshipPhasesQuery : IRequest<Result<PaginatedResult<GetInternshipPhasesResponse>>>
{
    public Guid? EnterpriseId { get; init; }
    public InternshipPhaseStatus? Status { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
