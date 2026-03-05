using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Evaluations.Commands.PublishEvaluation;

public record PublishEvaluationCommand : IRequest<Result<PublishEvaluationResponse>>
{
    [JsonIgnore]
    public Guid EvaluationId { get; init; }
}
