using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;


namespace IOCv2.Application.Features.EvaluationCycles.Commands.UpdateEvaluationCycle;

public record UpdateEvaluationCycleCommand : IRequest<Result<UpdateEvaluationCycleResponse>>
{
    [JsonIgnore]
    public Guid CycleId { get; init; }
    public string Name { get; init; } = null!;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public EvaluationCycleStatus Status { get; init; }
}

