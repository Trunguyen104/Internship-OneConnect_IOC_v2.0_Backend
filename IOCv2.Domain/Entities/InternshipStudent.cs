using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class InternshipStudent : BaseEntity
    {
        public Guid InternshipId { get; set; }
        public virtual InternshipGroup InternshipGroup { get; set; } = null!;

        public Guid StudentId { get; set; }
        public virtual Student Student { get; set; } = null!;

        public InternshipRole Role { get; set; }
        public InternshipStatus Status { get; set; }

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}
