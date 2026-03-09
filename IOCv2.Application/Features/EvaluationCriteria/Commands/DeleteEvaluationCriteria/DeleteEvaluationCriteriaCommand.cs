using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCriteria.Commands.DeleteEvaluationCriteria;

public record DeleteEvaluationCriteriaCommand(Guid CriteriaId) : IRequest<Result<bool>>;
