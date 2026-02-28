using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Commands.CreateProject
{
    public record CreateProjectCommand : IRequest<Result<CreateProjectResponse>>, IMapFrom<Project>
    {
        public Guid InternshipId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
