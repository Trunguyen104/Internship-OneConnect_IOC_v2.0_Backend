using System;
using IOCv2.Domain.Enums;

namespace IOCv2.Domain.Entities
{
    public class JobApplication : BaseEntity
    {
        public Guid ApplicationId { get; private set; }
        public Guid JobId { get; private set; }
        public Guid StudentId { get; private set; }
        public Guid CvId { get; private set; }
        public string? CoverLetter { get; private set; }
        public JobApplicationStatus Status { get; private set; }

        // Timestamps: AppliedAt stored explicitly; UpdatedAt is inherited from BaseEntity (UpdatedAt)
        public DateTime AppliedAt { get; private set; } = DateTime.UtcNow;

        // Navigation
        public virtual Job Job { get; set; } = null!;
        public virtual Student Student { get; set; } = null!;

        private JobApplication() { }

        public static JobApplication Create(Guid jobId, Guid studentId, Guid cvId, string? coverLetter = null)
        {
            return new JobApplication
            {
                ApplicationId = Guid.NewGuid(),
                JobId = jobId,
                StudentId = studentId,
                CvId = cvId,
                CoverLetter = coverLetter,
                Status = JobApplicationStatus.Applied,
                AppliedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void UpdateStatus(JobApplicationStatus status)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
        }

        public void UpdateCoverLetter(string? coverLetter)
        {
            CoverLetter = coverLetter;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}