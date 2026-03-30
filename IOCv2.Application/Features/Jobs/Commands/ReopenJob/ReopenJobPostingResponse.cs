using System;
using IOCv2.Domain.Enums;

namespace IOCv2.Application.Features.Jobs.Commands.ReopenJob
{
    public record ReopenJobPostingResponse
    {
        public Guid JobId { get; init; }
        public string? Title { get; init; }
        public JobStatus Status { get; init; }
    }
}
