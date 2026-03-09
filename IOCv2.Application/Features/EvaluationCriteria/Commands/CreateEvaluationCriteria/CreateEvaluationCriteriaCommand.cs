using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCriteria.Commands.CreateEvaluationCriteria;

public record CreateEvaluationCriteriaCommand : IRequest<Result<CreateEvaluationCriteriaResponse>>
{
    [JsonIgnore]
    public Guid CycleId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public decimal MaxScore { get; init; }
    public decimal Weight { get; init; }
}
