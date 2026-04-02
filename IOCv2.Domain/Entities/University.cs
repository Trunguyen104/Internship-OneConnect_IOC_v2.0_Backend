namespace IOCv2.Domain.Entities
{
    public class University : BaseEntity
    {
        public Guid UniversityId { get; private set; }
        public string Code { get; private set; } = null!;
        public string Name { get; private set; } = null!;
        public string? Address { get; private set; }
        public string? LogoUrl { get; private set; }
        public string? ContactEmail { get; private set; }

        public short Status { get; private set; } = 1; // 0=Inactive, 1=Active, 2=Suspended

        public virtual ICollection<UniversityUser> UniversityUsers { get; set; } = new List<UniversityUser>();
        public virtual ICollection<Term> Terms { get; set; } = new List<Term>();

        // Many-to-many: Universities <-> Jobs
        public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

        protected University() { }

        public static University Create(string code, string name, string? address, string? logoUrl, string? contactEmail = null)
        {
            return Create(code, name, address, logoUrl, Guid.NewGuid(), contactEmail);
        }

        public static University Create(string code, string name, string? address, string? logoUrl, Guid universityId, string? contactEmail = null)
        {
            return new University
            {
                UniversityId = universityId,
                Code = code,
                Name = name,
                Address = address,
                LogoUrl = logoUrl,
                ContactEmail = contactEmail,
                Status = 1,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void UpdateInfo(string code, string name, string? address, string? logoUrl, string? contactEmail = null)
        {
            Code = code;
            Name = name;
            Address = address;
            LogoUrl = logoUrl;
            ContactEmail = contactEmail;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Deactive()
        {
            Status = 0;
            UpdatedAt = DateTime.UtcNow;
        }
        
        public void Delete()
        {
            DeletedAt = DateTime.UtcNow;
        }
    }
}
