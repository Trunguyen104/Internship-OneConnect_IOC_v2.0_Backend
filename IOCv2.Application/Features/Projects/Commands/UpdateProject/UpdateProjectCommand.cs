using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Mappings;
using IOCv2.Application.Features.Projects.Commands.CreateProject;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    public record UpdateProjectCommand : IRequest<Result<UpdateProjectResponse>>, IMapFrom<Project>
    {
        public Guid ProjectId { get; set; }
        public Guid? InternshipId { get; set; }
        public string? ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ProjectStatus? Status { get; set; }
    }
}
