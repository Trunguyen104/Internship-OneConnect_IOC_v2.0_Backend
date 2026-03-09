namespace IOCv2.Domain.Entities;

public class EvaluationDetail : BaseEntity
{
    public Guid DetailId { get; set; }

    public Guid EvaluationId { get; set; }
    public virtual Evaluation Evaluation { get; set; } = null!;

    public Guid CriteriaId { get; set; }
    public virtual EvaluationCriteria Criteria { get; set; } = null!;

    /// <summary>Điểm cho tiêu chí này (0 → MaxScore của criteria)</summary>
    public decimal Score { get; set; }

    /// <summary>Nhận xét riêng cho tiêu chí này</summary>
    public string? Comment { get; set; }
}
