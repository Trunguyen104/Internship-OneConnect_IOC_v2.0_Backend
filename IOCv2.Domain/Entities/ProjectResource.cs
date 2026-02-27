using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Entities
{
    public class ProjectResources : BaseEntity
    {
        public Guid ProjectResourceId { get; set; }
        public Guid ProjectId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        public FileType ResourceType { get; set; }
        public string ResourceUrl { get; set; } = string.Empty;
        public virtual Project Project { get; set; } = null!;
        private ProjectResources()
        { }
        public ProjectResources (Guid projectId, string resourceName, FileType resourceType, string resourceUrl)
        {
            ProjectResourceId = Guid.NewGuid();
            ProjectId = projectId;
            ResourceName = resourceName;
            ResourceType = resourceType;
            ResourceUrl = resourceUrl;
        }
    }
}
