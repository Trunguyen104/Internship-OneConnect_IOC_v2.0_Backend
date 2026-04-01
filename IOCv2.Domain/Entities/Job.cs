using System;
using System.Collections.Generic;
using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class Job : BaseEntity
    {
        public Guid JobId { get; set; }
        public Guid EnterpriseId { get; set; }
        public Guid? InternshipPhaseId { get; set; }
        public string? Title { get; set; }
        public string? Position { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string? Location { get; set; }
        public string? Benefit { get; set; }
        public DateTime? ExpireDate { get; set; }
        public JobStatus? Status { get; set; }

        // New: internship date range
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // New: audience (public / targeted)
        public JobAudience? Audience { get; set; }

        // New: intern phase this job belongs to
        public Guid? InternPhaseId { get; set; }
        public virtual InternshipPhase? InternPhase { get; set; }

        // Navigation
        public virtual Enterprise Enterprise { get; set; } = null!;

        // Applications for this job (EF-mapped navigation)
        public virtual ICollection<InternshipApplication> InternshipApplications { get; set; } = new List<InternshipApplication>();

        // Many-to-many: Jobs <-> Universities
        public virtual ICollection<University> Universities { get; set; } = new List<University>();
        public virtual InternshipPhase InternshipPhase { get; set; } = null!;

        // Factory method for creating a Job (used by application layer)
        public static Job Create(
            Guid enterpriseId,
            Guid? internshipPhase,
            string title,
            string? description = null,
            string? requirements = null,
            string? benefit = null,
            string? location = null,
            DateTime? expireDate = null)
        {
            return new Job
            {
                JobId = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                InternshipPhaseId = internshipPhase,
                Title = title,
                Description = description,
                Requirements = requirements,
                Benefit = benefit,
                Location = location,
                ExpireDate = expireDate,
                Status = JobStatus.DRAFT
            };
        }
    }

}