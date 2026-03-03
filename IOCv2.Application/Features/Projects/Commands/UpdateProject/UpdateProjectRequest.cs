using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    public class UpdateProjectRequest
    {
        public Guid? InternshipId { get; set; }
        public string? ProjectName { get; set; } = default!;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Domain.Enums.ProjectStatus? Status { get; set; }
    }
}
