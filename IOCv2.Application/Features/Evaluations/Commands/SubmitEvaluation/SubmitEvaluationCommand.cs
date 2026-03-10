using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Evaluations.Commands.SubmitEvaluation;

public record SubmitEvaluationCommand : IRequest<Result<SubmitEvaluationResponse>>
{
    [JsonIgnore]
    public Guid CycleId { get; init; }

    [JsonIgnore]
    public Guid InternshipId { get; init; }

    /// <summary>
    /// Danh sách StudentId muốn nộp điểm. 
    /// Để null ở trong mảng đại diện cho điểm đánh giá của Nhóm (Group Evaluation).
    /// </summary>
    public List<Guid?> StudentIds { get; init; } = new();
}
