using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class UserCodeSequence
    {
        public UserRole Role { get; set; }
        public int CurrentNumber { get; set; }
    }
}
