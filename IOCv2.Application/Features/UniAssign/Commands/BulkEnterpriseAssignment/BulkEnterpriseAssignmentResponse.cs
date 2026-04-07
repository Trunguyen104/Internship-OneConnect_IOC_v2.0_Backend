using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkEnterpriseAssignment
{
    public record BulkEnterpriseAssignmentResponse
    {
        public string? Message { get; set; }
        public Guid TermId { get; set; }
        public Guid EnterpriseId { get; set; }
        public Guid InternPhaseId { get; set; }
        public List<Guid> StudentIds { get; set; } = new();
    }
}