namespace IOCv2.Domain.Entities
{
    public class University : BaseEntity
    {
        public Guid UniversityId { get; set; }
        public string Code { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public string? LogoUrl { get; set; }

        public short Status { get; set; } = 1; // 0=Inactive, 1=Active, 2=Suspended

        public virtual ICollection<UniversityUser> UniversityUsers { get; set; } = new List<UniversityUser>();
        public virtual ICollection<Term> Terms { get; set; } = new List<Term>();
    }
}
