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
        [System.Text.Json.Serialization.JsonIgnore]
        public Guid ProjectResourceId { get; init; }
        public Guid ProjectId { get; init; }
        public string ResourceName { get; init; } = string.Empty;
        /// <summary>
        /// Deprecated: file type is immutable after upload and this field is ignored.
        /// Kept for backward compatibility with existing clients.
        /// </summary>
        public FileType ResourceType { get; init; }
    }
}

