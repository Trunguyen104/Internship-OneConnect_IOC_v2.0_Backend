using IOCv2.Domain.Enums;
using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource
{
    public record UpdateProjectResourceCommand : IRequest<Result<UpdateProjectResourceResponse>>
    {
        public Guid ProjectResourceId { get; init; }
        public Guid ProjectId { get; init; }
        public string ResourceName { get; init; } = string.Empty;
        /// <summary>Type of file: DocumentFile, Image, Video, Other</summary>
        public FileType ResourceType { get; init; }
    }
}

