using IOCv2.Domain.Enums;
namespace IOCv2.Domain.Entities
{
    public class Logbook : BaseEntity
    {
        public Guid LogbookId { get; set; }
        public Guid InternshipId { get; set; }
        public virtual InternshipGroup InternshipGroup { get; set; } = null!;
        public Guid StudentId { get; set; }
        public virtual Student Student { get; set; } = null!;
        public required string Summary { get; set; } 
        public string? Issue { get; set; } = string.Empty;
        public required string Plan { get; set; }
        public DateTime DateReport { get; set; }
        public LogbookStatus Status { get; set; }

        public ICollection<WorkItem> WorkItem { get; set; } = new List<WorkItem>();

    }
}
