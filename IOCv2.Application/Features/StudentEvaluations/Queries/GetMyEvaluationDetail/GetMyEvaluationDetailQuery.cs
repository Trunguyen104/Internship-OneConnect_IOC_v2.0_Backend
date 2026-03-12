using System;
using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetMyEvaluationDetail;

public record GetMyEvaluationDetailQuery : IRequest<Result<GetMyEvaluationDetailResponse>>
{
    [JsonIgnore]
    public Guid CurrentUserId { get; init; }

    [JsonIgnore]
    public string Role { get; init; } = string.Empty;

    public Guid CycleId { get; init; }
}
