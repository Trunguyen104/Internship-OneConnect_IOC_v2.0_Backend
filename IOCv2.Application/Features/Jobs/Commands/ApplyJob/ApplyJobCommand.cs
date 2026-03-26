using IOCv2.Application.Common.Models;
using System;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Jobs.Commands.ApplyJob
{
    public record ApplyJobCommand() : MediatR.IRequest<Result<ApplyJobResponse>>
    {
        [JsonIgnore]
        public Guid JobId { get; init; }
    }
}
