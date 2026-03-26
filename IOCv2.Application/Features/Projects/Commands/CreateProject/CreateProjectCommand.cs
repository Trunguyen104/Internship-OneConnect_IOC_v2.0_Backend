using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Projects.Commands.CreateProject
{
    public class CreateProjectCommand : IRequest<Result<CreateProjectResponse>>
    {
        public Guid? InternshipId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // New fields
        public string Field { get; set; } = string.Empty;
        public string Requirements { get; set; } = string.Empty;
        public string? Deliverables { get; set; }
        public ProjectTemplate Template { get; set; } = ProjectTemplate.None;

        /// <summary>
        /// Tùy chọn — nếu null BE sẽ tự generate theo slugify algorithm.
        /// </summary>
        public string? ProjectCode { get; set; }
    }
}
