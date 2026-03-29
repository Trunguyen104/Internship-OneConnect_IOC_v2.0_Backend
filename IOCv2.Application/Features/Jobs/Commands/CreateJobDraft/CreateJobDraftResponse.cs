using IOCv2.Application.Extensions.Mappings;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.CreateJobDraft
{
    public class CreateJobDraftResponse : IMapFrom<Job>
    {
        public Guid JobId { get; set; }
        public Guid EnterpriseId { get; set; }
        public string? Title { get; set; }
        public string? Position { get; set; }
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string? Location { get; set; }
        public string? Benefit { get; set; }
        public int? Quantity { get; set; }
        public DateTime? ExpireDate { get; set; }

        // New: internship date range
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // New: audience (public / targeted)
        public JobAudience? Audience { get; set; }
        public JobStatus? Status { get; set; }
    }
}
