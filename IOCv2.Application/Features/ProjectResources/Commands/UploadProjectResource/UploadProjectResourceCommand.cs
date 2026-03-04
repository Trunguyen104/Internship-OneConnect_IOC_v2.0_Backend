using IOCv2.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.UploadProjectResource
{
    public class UploadProjectResourceCommand : IRequest<Result<UploadProjectResourceResponse>>
    {
        public Guid ProjectId { get; set; }
        public string ResourceName { get; set; } = string.Empty;
        /// <summary>Type of file: DocumentFile, Image, Video, Other</summary>
        public string ResourceType { get; set; } = string.Empty;
        public IFormFile File { get; set; } = null!;
    }
}
