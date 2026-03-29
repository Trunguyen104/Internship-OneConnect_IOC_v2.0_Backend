using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Projects.Commands.UnpublishProject
{
    public class UnpublishProjectResponse
    {
        public Guid ProjectId { get; set; }
        public VisibilityStatus VisibilityStatus { get; set; }
        public OperationalStatus OperationalStatus { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
