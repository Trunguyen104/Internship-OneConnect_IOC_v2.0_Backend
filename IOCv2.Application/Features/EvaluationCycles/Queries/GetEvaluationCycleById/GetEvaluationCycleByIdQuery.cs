using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycleById;

public record GetEvaluationCycleByIdQuery(Guid CycleId) : IRequest<Result<GetEvaluationCycleByIdResponse>>;
