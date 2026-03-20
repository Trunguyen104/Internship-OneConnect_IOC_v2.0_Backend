namespace IOCv2.Domain.Entities
{
    public class UniversityUser : BaseEntity
    {
        public Guid UniversityUserId { get; set; }
        
        public Guid UniversityId { get; set; }
        public virtual University University { get; set; } = null!;

        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        public string? Position { get; set; }
        public string? Bio { get; private set; }
        public string? Department { get; private set; }

        public void UpdateMetadata(string? bio, string? department)
        {
            Bio = bio;
            Department = department;
        }
    }
}
