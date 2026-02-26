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
        public List<ProjectResources> ProjectResources { get; set; } = new();
        public ICollection<StudentProject> StudentProjects { get; set; } = new List<StudentProject>();
        public Project(Guid internshipId, string projectName, string description)
        {
            ProjectId = Guid.NewGuid();
            InternshipId = internshipId;
            ProjectName = projectName;
            Description = description;
            Status = ProjectStatus.Planning;
        }
    }
}
