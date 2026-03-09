using IOCv2.Domain.Enums;
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
    public record UploadProjectResourceCommand : IRequest<Result<UploadProjectResourceResponse>>
    {
        public Guid ProjectId { get; init; }
        public string ResourceName { get; init; } = string.Empty;
        public IFormFile File { get; init; } = null!;
    }
}

