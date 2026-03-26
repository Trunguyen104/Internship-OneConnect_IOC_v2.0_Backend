using System.ComponentModel.DataAnnotations;

namespace IOCv2.Domain.Entities;

public class EvaluationCriteria : BaseEntity
{
    [Key]
    public Guid CriteriaId { get; set; }

    public Guid CycleId { get; set; }
    public virtual EvaluationCycle Cycle { get; set; } = null!;

    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public decimal MaxScore { get; set; }
    public decimal Weight { get; set; }
}
