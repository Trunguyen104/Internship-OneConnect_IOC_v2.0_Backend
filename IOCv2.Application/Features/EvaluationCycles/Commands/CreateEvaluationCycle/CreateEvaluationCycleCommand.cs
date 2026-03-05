using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.CreateEvaluationCycle;

public record CreateEvaluationCycleCommand : IRequest<Result<CreateEvaluationCycleResponse>>
{
    public Guid TermId { get; init; }
    public string Name { get; init; } = null!;
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
}
