using System;

namespace IOCv2.Application.Features.Jobs.Queries.GetJobById
{
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
    }
}
