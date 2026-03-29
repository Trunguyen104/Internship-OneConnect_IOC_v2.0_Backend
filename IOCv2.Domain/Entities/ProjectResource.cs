using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class ProjectResources : BaseEntity
    {
        [Key]
        public Guid ProjectResourceId { get; set; }
        public Guid ProjectId { get; set; }
        public string? ResourceName { get; set; }
        public FileType ResourceType { get; set; }
        public string ResourceUrl { get; set; } = null!;
        public Guid? UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
        public virtual EnterpriseUser? UploadedByUser { get; set; }
        public virtual Project Project { get; set; } = null!;
        public ProjectResources()
        { }
        public ProjectResources (Guid projectId, string resourceName, FileType resourceType, string resourceUrl)
        {
            ProjectResourceId = Guid.NewGuid();
            ProjectId = projectId;
            ResourceName = resourceName;
            ResourceType = resourceType;
            ResourceUrl = resourceUrl;
        }

        public void UpdateInfo(Guid projectId, string? resourceName, FileType resourceType)
        {
            ProjectId = projectId;
            ResourceName = resourceName;
            ResourceType = resourceType;
        }
    }
}
