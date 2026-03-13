using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetStudentTeamEvaluations;

public record GetStudentTeamEvaluationsQuery : IRequest<Result<List<GetStudentTeamEvaluationsResponse>>>
{
    [JsonIgnore]
    public Guid CurrentUserId { get; init; }

    [JsonIgnore]
    public string Role { get; init; } = string.Empty;

    public Guid CycleId { get; init; }
}
