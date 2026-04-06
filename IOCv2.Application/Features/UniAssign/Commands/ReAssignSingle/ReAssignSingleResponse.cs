using System;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.UniAssign.Commands.ReAssignSingle
{
    public class ReAssignSingleResponse
    {
        public Guid OldApplicationId { get; set; }
        public Guid NewApplicationId { get; set; }
        public InternshipApplicationStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
