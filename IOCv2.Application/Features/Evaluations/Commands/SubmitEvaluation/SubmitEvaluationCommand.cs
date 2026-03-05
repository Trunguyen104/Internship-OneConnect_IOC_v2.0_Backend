using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Evaluations.Commands.SubmitEvaluation;

public record SubmitEvaluationCommand : IRequest<Result<SubmitEvaluationResponse>>
{
    [JsonIgnore]
    public Guid EvaluationId { get; init; }
}
