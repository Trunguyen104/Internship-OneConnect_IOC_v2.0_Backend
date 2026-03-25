using System;
using System.Collections.Generic;
using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Job : BaseEntity
    {
        public Guid JobId { get; set; }
        public Guid EnterpriseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string? Location { get; set; }
        public int? InternshipDuration { get; set; }
        public string? Benefit { get; set; }
        public int? Quantity { get; set; }
        public DateTime? ExpireDate { get; set; }
        public JobStatus Status { get; set; }

        // Navigation
        public virtual Enterprise Enterprise { get; set; } = null!;

        // Added: applications for this job
        public virtual ICollection<JobApplication> JobApplications { get; set; } = new List<JobApplication>();

        // Factory method for creating a Job (used by application layer)
        public static Job Create(
            Guid enterpriseId,
            string title,
            string? description = null,
            string? requirements = null,
            string? benefit = null,
            string? location = null,
            int? quantity = null,
            DateTime? expireDate = null)
        {
            return new Job
            {
                JobId = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                Title = title,
                Description = description,
                Requirements = requirements,
                Benefit = benefit,
                Location = location,
                Quantity = quantity,
                ExpireDate = expireDate,
                Status = JobStatus.DRAFT
            };
        }
    }
}