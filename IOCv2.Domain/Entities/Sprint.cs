using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Sprint : BaseEntity
{
    public Guid SprintId { get; private set; }
    public Guid ProjectId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Goal { get; private set; }

    public DateOnly? StartDate { get; private set; }
    public DateOnly? EndDate { get; private set; }

    public SprintStatus Status { get; private set; }

    // Navigation properties
    public virtual Project Project { get; private set; } = null!;
    public virtual ICollection<SprintWorkItem> SprintWorkItems { get; private set; } = new List<SprintWorkItem>();

    /// <summary>
    /// Default constructor for EF Core.
    /// </summary>
    public Sprint() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Sprint"/> class.
    /// </summary>
    public Sprint(Guid projectId, string name, string? goal)
    {
        SprintId = Guid.NewGuid();
        ProjectId = projectId;
        Name = name;
        Goal = goal;
        Status = SprintStatus.Planned;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Updates core sprint details.
    /// </summary>
    public void Update(string name, string? goal, DateOnly? startDate, DateOnly? endDate)
    {
        Name = name;
        Goal = goal;
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the sprint to Active status.
    /// </summary>
    public void Start(DateOnly startDate, DateOnly endDate)
    {
        Status = SprintStatus.Active;
        StartDate = startDate;
        EndDate = endDate;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the sprint to Completed status.
    /// </summary>
    public void Complete()
    {
        Status = SprintStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
    }
}
