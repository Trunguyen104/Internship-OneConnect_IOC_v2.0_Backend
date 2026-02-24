namespace IOCv2.Domain.Entities
{
    public class Internship
    {
        public Guid InternshipId { get; set; }
        public Guid TermId { get; set; }
        public virtual Term Term { get; set; } = null!;
        public Guid StudentId { get; set; }
        public virtual Student Student { get; set; } = null!;
        public Guid JobId { get; set; }
        public virtual Job Job { get; set; } = null!;
        public Guid MentorId { get; set; }
        public virtual EnterpriseUser Mentor { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}