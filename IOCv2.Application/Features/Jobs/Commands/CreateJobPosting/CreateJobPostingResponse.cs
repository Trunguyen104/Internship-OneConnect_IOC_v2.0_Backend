using IOCv2.Application.Extensions.Mappings;
using System;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobPosting
{
    public class CreateJobPostingResponse : IMapFrom<Job>
    {
        public Guid JobId { get; set; }
        public Guid EnterpriseId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public JobAudience Audience { get; set; }
        public JobStatus Status { get; set; }
    }
}
