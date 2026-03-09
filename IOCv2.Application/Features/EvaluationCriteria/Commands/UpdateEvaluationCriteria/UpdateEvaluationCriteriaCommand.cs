using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.EvaluationCriteria.Commands.UpdateEvaluationCriteria;

public record UpdateEvaluationCriteriaCommand : IRequest<Result<UpdateEvaluationCriteriaResponse>>
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid CriteriaId { get; init; }
    public string Name { get; init; } = null!;
    public string? Description { get; init; }
    public decimal MaxScore { get; init; }
    public decimal Weight { get; init; }
}
