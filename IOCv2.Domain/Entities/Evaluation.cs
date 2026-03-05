using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Evaluation : BaseEntity
{
    public Guid EvaluationId { get; set; }

    /// <summary>FK → EvaluationCycle</summary>
    public Guid CycleId { get; set; }
    public virtual EvaluationCycle Cycle { get; set; } = null!;

    /// <summary>FK → InternshipGroup (nhóm thực tập)</summary>
    public Guid InternshipId { get; set; }
    public virtual InternshipGroup Internship { get; set; } = null!;

    /// <summary>FK → Student (sinh viên được chấm) — null nếu là đánh giá cả nhóm</summary>
    public Guid? StudentId { get; set; }
    public virtual Student? Student { get; set; }

    /// <summary>FK → User (Mentor/người chấm)</summary>
    public Guid EvaluatorId { get; set; }
    public virtual User Evaluator { get; set; } = null!;

    public EvaluationStatus Status { get; set; } = EvaluationStatus.Draft;

    /// <summary>Tổng điểm sau khi tính weighted score từ các EvaluationDetail</summary>
    public decimal? TotalScore { get; set; }

    /// <summary>Nhận xét chung của Mentor</summary>
    public string? Note { get; set; }

    public virtual ICollection<EvaluationDetail> Details { get; set; } = new List<EvaluationDetail>();
}
