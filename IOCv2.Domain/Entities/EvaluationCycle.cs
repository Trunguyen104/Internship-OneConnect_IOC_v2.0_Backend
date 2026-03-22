using IOCv2.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace IOCv2.Domain.Entities;

public class EvaluationCycle : BaseEntity
{
    [Key]
    public Guid CycleId { get; set; }

    public Guid TermId { get; set; }
    public virtual Term Term { get; set; } = null!;

    public string Name { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public EvaluationCycleStatus Status { get; set; } = EvaluationCycleStatus.Pending;

    public virtual ICollection<EvaluationCriteria> Criteria { get; set; } = new List<EvaluationCriteria>();
}
