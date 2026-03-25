using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobById
{
    public class ApplicationStatusCountDto
    {
        // JobApplicationStatus as short (matches enum values)
        public short Status { get; set; }
        public string? StatusName { get; set; }
        public int Count { get; set; }
    }

    public class GetJobByIdResponse
    {
        public Guid JobId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Requirements { get; set; }
        public string? Location { get; set; }
        public DateTime? ExpireDate { get; set; }

        // Enterprise info
        public Guid EnterpriseId { get; set; }
        public string? EnterpriseName { get; set; }
        public string? EnterpriseLogoUrl { get; set; }

        // Apply controls (for UI)
        public bool CanApply { get; set; }
        public string? ApplyDisabledReason { get; set; }

        // New: application counts per status (e.g., Applied, Interview, Offered, Rejected, Accepted)
        public List<ApplicationStatusCountDto> ApplicationCounts { get; set; } = new List<ApplicationStatusCountDto>();

        // New: list of allowed actions for the current user (e.g., "Edit","Publish","Close","Delete","ViewApplications","Apply")
        public List<string> AllowedActions { get; set; } = new List<string>();
    }
}
