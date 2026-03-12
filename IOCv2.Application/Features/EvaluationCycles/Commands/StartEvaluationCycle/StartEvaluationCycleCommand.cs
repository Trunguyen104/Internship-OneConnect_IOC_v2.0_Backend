using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.StartEvaluationCycle;

public record StartEvaluationCycleCommand : IRequest<Result<StartEvaluationCycleResponse>>
{
    [JsonIgnore]
    public Guid CycleId { get; init; }
}
