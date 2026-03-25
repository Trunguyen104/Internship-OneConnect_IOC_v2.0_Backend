using System;

namespace IOCv2.Application.Features.Jobs.Commands.PublishJob
{
    public class PublishJobResponse
    {
        public Guid JobId { get; set; }
        public short Status { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}