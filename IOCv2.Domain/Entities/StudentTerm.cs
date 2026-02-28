namespace IOCv2.Domain.Entities;

public class StudentTerm : BaseEntity
{
    public Guid TermId { get; set; }
    public Guid StudentId { get; set; }
    public short? Status { get; set; }

    public virtual Term Term { get; set; } = null!;
    public virtual Student Student { get; set; } = null!;
}
