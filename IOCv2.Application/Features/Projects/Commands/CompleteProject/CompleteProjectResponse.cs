using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Projects.Commands.CompleteProject
{
    public class CompleteProjectResponse
    {
        public Guid ProjectId { get; set; }
        public ProjectStatus Status { get; set; }
        public int PendingStudentsCount { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
