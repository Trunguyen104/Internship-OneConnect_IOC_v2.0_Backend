using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Projects.Commands.UpdateProject;
using IOCv2.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Commands.DeleteProject
{
    public record DeleteProjectCommand : IRequest<Result<string>>
    {
        public Guid ProjectId { get; set; }
    }
}
