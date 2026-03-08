using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCriteria.Queries.GetEvaluationCriteria;

public record GetEvaluationCriteriaQuery(Guid CycleId) : IRequest<Result<List<GetEvaluationCriteriaResponse>>>;
