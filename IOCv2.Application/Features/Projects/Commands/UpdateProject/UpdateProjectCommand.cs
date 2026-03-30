using IOCv2.Application.Common.Models;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    public class UpdateProjectResourceInput
    {
        public Guid ProjectResourceId { get; set; }
        public string? ResourceName { get; set; }
        public string? ExternalUrl { get; set; }
    }

    public class UpdateProjectLinkInput
    {
        public string ResourceName { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
    }

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

        // Optional resource operations handled in the same update transaction.
        public List<IFormFile>? Files { get; set; }
        public List<UpdateProjectLinkInput>? Links { get; set; }
        public List<UpdateProjectResourceInput>? ResourceUpdates { get; set; }
        public List<Guid>? ResourceDeleteIds { get; set; }
    }
}
