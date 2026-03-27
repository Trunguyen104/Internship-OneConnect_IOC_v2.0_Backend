using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Projects.Commands.CompleteProject
{
    public class CompleteProjectResponse
    {
        public Guid ProjectId { get; set; }
        public OperationalStatus OperationalStatus { get; set; }
        public int PendingStudentsCount { get; set; }
        public bool InternPhaseEndWarning { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
