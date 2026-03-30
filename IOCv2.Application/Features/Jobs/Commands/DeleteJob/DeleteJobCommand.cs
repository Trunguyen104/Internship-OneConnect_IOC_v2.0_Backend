using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Jobs.Commands.DeleteJob
{
    public record DeleteJobCommand : IRequest<Result<DeleteJobResponse>>
    {
        [JsonIgnore]
        public Guid JobId { get; init; }

        /// <summary>
        /// If the job has active applications and this flag is true, proceed with delete.
        /// If false and active applications exist, handler returns a warning (no DB change).
        /// </summary>
        public bool ConfirmWhenHasActiveApplications { get; init; } = false;
    }
}