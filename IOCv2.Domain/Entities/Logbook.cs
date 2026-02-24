using IOCv2.Domain.Enums;
namespace IOCv2.Domain.Entities
{
    public class Logbook : BaseEntity
    {
        public Guid LogbookId { get; set; }
        public Guid InternshipId { get; set; }
        public virtual Internship Internship { get; set; } = null!;
        public string Content { get; set; }
        public string? Issue { get; set; } = string.Empty;
        public LogbookStatus Status { get; set; }
    }
}
