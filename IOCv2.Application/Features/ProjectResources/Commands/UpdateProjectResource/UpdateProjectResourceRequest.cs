using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource
{
    public class UpdateProjectResourceRequest
    {
        public Guid ProjectId { get; set; }
        public string ResourceName { get; set; } = default!;
        public FileType ResourceType { get; set; }
    }
}
