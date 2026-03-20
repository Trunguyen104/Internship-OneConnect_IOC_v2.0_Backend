using System;
using System.Collections.Generic;
using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Student : BaseEntity
    {
        public Guid StudentId { get; set; }
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string? ClassName { get; set; }
        public string? Major { get; set; }
        public decimal? Gpa { get; set; }
        public string? HighestDegree { get; set; }

        public StudentStatus InternshipStatus { get; set; }

        // Navigation properties
        public virtual ICollection<StudentTerm> StudentTerms { get; set; } = new List<StudentTerm>();
        public virtual ICollection<InternshipStudent> InternshipStudents { get; set; } = new List<InternshipStudent>();
        public virtual ICollection<InternshipApplication> InternshipApplications { get; set; } = new List<InternshipApplication>();
        public virtual ICollection<Logbook> Logbooks { get; set; } = new List<Logbook>();
        public virtual ICollection<WorkItem> WorkItems { get; set; } = new List<WorkItem>();

        // Added: job applications by this student
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}
