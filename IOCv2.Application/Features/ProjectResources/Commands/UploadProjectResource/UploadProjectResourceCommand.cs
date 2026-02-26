using IOCv2.Application.Common.Models;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
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
        public string ResourceName { get; set; }
        public FileType ResourceType { get; set; }
        public IFormFile File { get; set; }
    }
}
