using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Projects.Commands.PublishProject
{
    public class PublishProjectResponse
    {
        public Guid ProjectId { get; set; }
        public ProjectStatus Status { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
