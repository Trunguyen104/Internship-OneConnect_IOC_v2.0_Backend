using IOCv2.Domain.Enums;
using System;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJobApplicationStatus
{
    public class UpdateInternshipApplicationStatusResponse
    {
        public Guid ApplicationId { get; set; }
        public InternshipApplicationStatus Status { get; set; }
        public string? Message { get; set; }
    }
}
