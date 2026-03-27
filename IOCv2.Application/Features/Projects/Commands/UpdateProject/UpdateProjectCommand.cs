using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    public class UpdateProjectCommand : IRequest<Result<UpdateProjectResponse>>
    {
        [JsonIgnore]
        public Guid ProjectId { get; set; }
        public string? ProjectName { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Status không được phép thay đổi qua UpdateProject.
        // Dùng PublishProject / CompleteProject / ArchiveProject để chuyển trạng thái.
        // Group assignment giờ qua AssignGroup/SwapGroup riêng.

        // New fields
        public string? Field { get; set; }
        public string? Requirements { get; set; }
        public string? Deliverables { get; set; }
        public ProjectTemplate? Template { get; set; }
    }
}
