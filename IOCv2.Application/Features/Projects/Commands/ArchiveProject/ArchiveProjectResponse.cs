using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Projects.Commands.ArchiveProject
{
    public class ArchiveProjectResponse
    {
        public Guid ProjectId { get; set; }
        public VisibilityStatus VisibilityStatus { get; set; }
        public OperationalStatus OperationalStatus { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
