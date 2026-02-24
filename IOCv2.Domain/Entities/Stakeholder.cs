using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Stakeholder : BaseEntity
    {
        public Guid Id { get; set; }
        public Guid ProjectId { get; set; }
        public string Name { get; set; } = null!;
        public StakeholderType Type { get; set; } = StakeholderType.Real;
        public string? Role { get; set; }
        public string? Description { get; set; }
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }

        public virtual Project Project { get; set; } = null!;
    }
}
