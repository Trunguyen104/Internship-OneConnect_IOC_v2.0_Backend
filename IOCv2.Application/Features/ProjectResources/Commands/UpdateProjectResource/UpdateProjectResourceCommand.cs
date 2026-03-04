using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource
{
    public class UpdateProjectResourceCommand : IRequest<Result<UpdateProjectResourceResponse>>
    {
        public Guid ProjectResourceId { get; set; }
        public Guid ProjectId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        /// <summary>Type of file: DocumentFile, Image, Video, Other</summary>
        public string ResourceType { get; set; } = string.Empty;
    }
}
