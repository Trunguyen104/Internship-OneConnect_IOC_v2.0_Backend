using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Evaluations.Queries.GetInternshipEvaluations;

public record GetInternshipEvaluationsQuery : IRequest<Result<GetInternshipEvaluationsResponse>>
{
    public Guid CycleId { get; init; }
    public Guid InternshipId { get; init; }
}
