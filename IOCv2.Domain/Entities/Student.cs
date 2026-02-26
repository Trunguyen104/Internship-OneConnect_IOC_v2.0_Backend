using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Student : BaseEntity
    {
        public Guid StudentId { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string? Class { get; set; }
        public string? Major { get; set; }
        public decimal? Gpa { get; set; }
        public string? HighestDegree { get; set; }

        public StudentStatus Status { get; set; }
        public virtual ICollection<Logbook> Logbooks { get; set; } = new List<Logbook>();
    }
}
