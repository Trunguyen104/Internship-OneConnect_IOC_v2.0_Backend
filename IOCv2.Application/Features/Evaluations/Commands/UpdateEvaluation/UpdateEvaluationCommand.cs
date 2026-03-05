using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Evaluations.Commands.UpdateEvaluation;

public record UpdateEvaluationCommand : IRequest<Result<UpdateEvaluationResponse>>
{
    [JsonIgnore]
    public Guid EvaluationId { get; init; }

    /// <summary>Nhận xét chung (cập nhật)</summary>
    public string? Note { get; init; }

    /// <summary>Danh sách điểm cập nhật theo từng tiêu chí</summary>
    public List<UpdateEvaluationDetailInput> Details { get; init; } = new();
}

public record UpdateEvaluationDetailInput
{
    public Guid CriteriaId { get; init; }
    public decimal Score { get; init; }
    public string? Comment { get; init; }
}
