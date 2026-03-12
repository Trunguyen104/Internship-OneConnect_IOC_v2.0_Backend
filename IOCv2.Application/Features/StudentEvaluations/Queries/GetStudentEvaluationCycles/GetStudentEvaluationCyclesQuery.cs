using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.StudentEvaluations.Queries.GetStudentEvaluationCycles;

public record GetStudentEvaluationCyclesQuery : IRequest<Result<List<GetStudentEvaluationCyclesResponse>>>
{
    [JsonIgnore]
    public Guid CurrentUserId { get; init; }
    
    [JsonIgnore]
    public string Role { get; init; } = string.Empty;

    public Guid InternshipId { get; init; }
}
