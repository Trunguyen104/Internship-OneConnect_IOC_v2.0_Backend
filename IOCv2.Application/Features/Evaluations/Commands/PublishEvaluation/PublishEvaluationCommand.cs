using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Evaluations.Commands.PublishEvaluation;

public record PublishEvaluationCommand : IRequest<Result<PublishEvaluationResponse>>
{
    [JsonIgnore]
    public Guid CycleId { get; init; }

    [JsonIgnore]
    public Guid InternshipId { get; init; }

    public List<Guid>? StudentIds { get; init; }
}
