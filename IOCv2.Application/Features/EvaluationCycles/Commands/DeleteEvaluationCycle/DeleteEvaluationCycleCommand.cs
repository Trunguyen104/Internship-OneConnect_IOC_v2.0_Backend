using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.DeleteEvaluationCycle;

public record DeleteEvaluationCycleCommand(Guid CycleId) : IRequest<Result<bool>>;
