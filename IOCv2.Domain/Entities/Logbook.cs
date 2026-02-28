using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Logbook : BaseEntity
{
    public Guid Id { get; set; }
    public Guid InternshipId { get; set; }
    public Guid? StudentId { get; set; }
    public string Content { get; set; } = null!;
    public LogbookStatus Status { get; set; }

    public virtual InternshipGroup InternshipGroup { get; set; } = null!;
    public virtual Student? Student { get; set; }
}
