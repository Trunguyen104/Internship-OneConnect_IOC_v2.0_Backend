using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkAssign
{
    public record BulkReassignEnterpriseResponse
    {
        public string? Message { get; init; }
        public int AssignedCount { get; init; } = 0;
        public List<Guid>? AssignedStudentIds { get; init; } = new();
    }
}