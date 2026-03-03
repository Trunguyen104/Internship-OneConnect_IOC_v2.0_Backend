using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities;

public class Logbook : BaseEntity
{
    public Guid LogbookId { get; private set; }
    public Guid ProjectId { get; private set; }
    public Guid? StudentId { get; private set; }
    public DateTime DateReport { get; private set; }
    public string Summary { get; private set; } = string.Empty;
    public string? Issue { get; private set; }
    public string Plan { get; private set; } = string.Empty;
    public LogbookStatus Status { get; private set; }

    public virtual Project Project { get; set; } = null!;
    public virtual Student? Student { get; set; }
    public virtual ICollection<WorkItem> WorkItem { get; set; } = new List<WorkItem>();

    public static Logbook Create(Guid projectId, Guid studentId, string summary, string? issue, string plan, DateTime dateReport)
    {
        var logbook = new Logbook
        {
            LogbookId = Guid.NewGuid(),
            ProjectId = projectId,
            StudentId = studentId,
            Summary = summary,
            Issue = issue,
            Plan = plan,
            DateReport = dateReport,
            CreatedAt = DateTime.UtcNow
        };

        logbook.DetermineStatus();
        return logbook;
    }

    public void Update(string summary, string? issue, string plan, DateTime dateReport)
    {
        Summary = summary;
        Issue = issue;
        Plan = plan;
        DateReport = dateReport;
        UpdatedAt = DateTime.UtcNow;

        DetermineStatus();
    }

    private void DetermineStatus()
    {
        // Status logic based on comparison between report date and creation date
        Status = DateReport.Date == CreatedAt.Date 
            ? LogbookStatus.PUNCTUAL 
            : LogbookStatus.LATE;
    }
}
