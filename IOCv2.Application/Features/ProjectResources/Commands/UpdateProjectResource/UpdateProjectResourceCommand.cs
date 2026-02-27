using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
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
        public string ResourceName { get; set; }
        public FileType ResourceType { get; set; }
    }
}
