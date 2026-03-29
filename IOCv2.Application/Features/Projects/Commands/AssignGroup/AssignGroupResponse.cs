using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Projects.Commands.AssignGroup
{
    public class AssignGroupResponse
    {
        public Guid ProjectId { get; set; }
        public Guid InternshipId { get; set; }
        public VisibilityStatus VisibilityStatus { get; set; }
        public OperationalStatus OperationalStatus { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
