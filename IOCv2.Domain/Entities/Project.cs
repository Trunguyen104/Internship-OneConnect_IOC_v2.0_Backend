namespace IOCv2.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    
    // Navigation properties
    public virtual ICollection<Stakeholder> Stakeholders { get; set; } = new List<Stakeholder>();
}

