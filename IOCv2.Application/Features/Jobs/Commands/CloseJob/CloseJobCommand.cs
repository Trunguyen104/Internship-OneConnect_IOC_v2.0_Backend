using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Jobs.Commands.CloseJob
{
    public record CloseJobCommand : IRequest<Result<CloseJobResponse>>
    {
        [JsonIgnore]
        public Guid JobId { get; init; }

        /// <summary>
        /// If the job has active applications, set this to true to confirm closing.
        /// If false and active applications exist, handler returns a warning (no DB change).
        /// </summary>
        public bool ConfirmWhenHasActiveApplications { get; init; } = false;
    }
}