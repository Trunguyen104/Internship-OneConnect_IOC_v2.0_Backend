using System;
using System.Collections.Generic;
using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Job : BaseEntity
    {
        public Guid JobId { get; private set; }
        public Guid EnterpriseId { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public string? Requirements { get; private set; }
        public string? Location { get; private set; }
        public int? InternshipDuration { get; private set; }
        public string? Benefit { get; private set; }
        public int? Quantity { get; private set; }
        public DateTime? ExpireDate { get; private set; }
        public JobStatus Status { get; private set; }

        // Navigation
        public virtual Enterprise Enterprise { get; set; } = null!;

        // Added: applications for this job
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();
    }
}