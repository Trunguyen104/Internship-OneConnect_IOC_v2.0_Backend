using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Evaluations.Commands.SaveEvaluations;

public record SaveEvaluationsCommand : IRequest<Result<List<SaveEvaluationsResponse>>>
{
    [JsonIgnore]
    public Guid CycleId { get; init; }

    /// <summary>ID của nhóm thực tập</summary>
    [JsonIgnore]
    public Guid InternshipId { get; init; }

    /// <summary>ID của Mentor/người chấm (thường lấy từ current user)</summary>
    [JsonIgnore]
    public Guid EvaluatorId { get; init; }

    /// <summary>Danh sách đánh giá cho từng sinh viên (hoặc 1 null cho nhóm)</summary>
    public List<StudentEvaluationInput> Evaluations { get; init; } = new();
}

public record StudentEvaluationInput
{
    /// <summary>
    /// ID của sinh viên được chấm điểm.
    /// Để null nếu muốn đánh giá cả nhóm (Group Evaluation).
    /// </summary>
    public Guid? StudentId { get; init; }

    /// <summary>Nhận xét chung</summary>
    public string? Note { get; init; }

    /// <summary>Danh sách điểm theo từng tiêu chí</summary>
    public List<EvaluationDetailInput> Details { get; init; } = new();
}

public record EvaluationDetailInput
{
    public Guid CriteriaId { get; init; }
    public decimal Score { get; init; }
    public string? Comment { get; init; }
}
