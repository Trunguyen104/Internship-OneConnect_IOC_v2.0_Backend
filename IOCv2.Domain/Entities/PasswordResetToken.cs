using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class PasswordResetToken
    {
        public Guid PasswordResetTokenId { get; set; }
        public Guid UserId { get; set; }              // user_id
        public string TokenHash { get; set; } = default!; // token_hash (64)
        public DateTimeOffset ExpiresAt { get; set; }     // expires_at
        public DateTimeOffset? UsedAt { get; set; }       // used_at
        public bool IsUsed => UsedAt.HasValue;
        public DateTime CreatedAt { get; set; }
        public User User { get; set; } = default!;
    }
}
