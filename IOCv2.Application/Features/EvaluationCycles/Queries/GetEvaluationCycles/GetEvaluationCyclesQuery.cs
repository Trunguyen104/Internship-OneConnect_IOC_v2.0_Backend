using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycles;

public record GetEvaluationCyclesQuery : IRequest<Result<List<GetEvaluationCyclesResponse>>>
{
    public Guid TermId { get; init; }
}
