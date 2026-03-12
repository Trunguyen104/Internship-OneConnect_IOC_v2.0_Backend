using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.CompleteEvaluationCycle;

public record CompleteEvaluationCycleCommand : IRequest<Result<CompleteEvaluationCycleResponse>>
{
    [JsonIgnore]
    public Guid CycleId { get; init; }
}
