using System;

namespace IOCv2.Application.Features.Jobs.Commands.CloseJob
{
    public class CloseJobResponse
    {
        public Guid JobId { get; set; }
        public short Status { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
