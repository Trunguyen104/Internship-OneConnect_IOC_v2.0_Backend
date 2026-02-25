using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class Project : BaseEntity
    {
        public Guid ProjectId { get; set; }
        public Guid InternshipId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectStatus? Status { get; set; }

        // Navigation Properties
        public virtual Internship Internship { get; set; } = null!;
        public List<ProjectResources> ProjectResources { get; set; } = new();
        public Project(Guid internshipId, string projectName, string description, Guid? createdBy)
        {
            ProjectId = Guid.NewGuid();
            InternshipId = internshipId;
            ProjectName = projectName;
            Description = description;
            Status = ProjectStatus.Planning;

            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy;
        }

        public void Update(string projectName, string description, DateTime? startDate, DateTime? endDate, Guid? updatedBy)
        {
            ProjectName = projectName;
            Description = description;
            StartDate = startDate;
            EndDate = endDate;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        public void ChangeStatus(ProjectStatus status, Guid? updatedBy)
        {
            Status = status;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }


    }
}
