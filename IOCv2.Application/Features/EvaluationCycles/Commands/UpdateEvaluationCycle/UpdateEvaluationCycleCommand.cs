using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.UpdateEvaluationCycle;

public record UpdateEvaluationCycleCommand : IRequest<Result<UpdateEvaluationCycleResponse>>
{
    public Guid CycleId { get; init; }
    public string Name { get; init; } = null!;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public string Status { get; init; } = null!;
}
