using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Enterprise : BaseEntity
    {
        public Guid EnterpriseId { get; set; }
        public string? TaxCode { get; set; }
        public string Name { get; set; } = null!;
        public string? Industry { get; set; }
        public string? Description { get; set; }
        public string? Address { get; set; }
        public string? Website { get; set; }
        public string? LogoUrl { get; set; }
        public string? BackgroundUrl { get; set; }
        public bool IsVerified { get; set; } = false;
        public short Status { get; set; } = 1; // 0=Inactive, 1=Active, 2=Suspended
        public virtual ICollection<EnterpriseUser> EnterpriseUsers { get; set; } = new List<EnterpriseUser>();
        public virtual ICollection<InternshipGroup> InternshipGroups { get; set; } = new List<InternshipGroup>();
    }
}
