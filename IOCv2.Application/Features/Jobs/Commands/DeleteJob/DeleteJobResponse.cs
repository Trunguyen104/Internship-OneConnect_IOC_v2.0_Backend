using System;

namespace IOCv2.Application.Features.Jobs.Commands.DeleteJob
{
    public class DeleteJobResponse
    {
        public Guid JobId { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
