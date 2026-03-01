using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class InternshipStudents : BaseEntity
    {
        public Guid InternshipId { get; set; }
        public Guid StudentId { get; set; }
        public InternshipStudentRole Role { get; set; }
        public InternshipStudentStatus Status { get; set; }
        // Navigation properties
        public virtual InternshipGroup InternshipGroup { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;

        public InternshipStudents() { }

        public InternshipStudents(Guid internshipId, Guid studentId, InternshipStudentRole role, InternshipStudentStatus status)
        {
            InternshipId = internshipId;
            StudentId = studentId;
            Role = role;
            Status = status;
            CreatedAt = DateTime.UtcNow;
        }
    }
}
