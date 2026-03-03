using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.DeleteProjectResource
{
    public class DeleteProjectResourceCommand : IRequest<Result<DeleteProjectResourceResponse>>
    {
        public Guid ResourceId { get; set; }
    }
}
