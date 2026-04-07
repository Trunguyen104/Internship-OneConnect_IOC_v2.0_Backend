using System;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.UniAssign.Commands.UnAssignSingle
{
    public class UnAssignSingleResponse
    {
        public Guid ApplicationId { get; set; }
        public InternshipApplicationStatus Status { get; set; }
        public string StatusLabel { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
