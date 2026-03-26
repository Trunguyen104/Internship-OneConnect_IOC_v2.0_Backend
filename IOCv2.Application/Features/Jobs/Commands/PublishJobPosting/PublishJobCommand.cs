using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Text.Json.Serialization;

namespace IOCv2.Application.Features.Jobs.Commands.PublishJob
{
    public record PublishJobCommand : IRequest<Result<PublishJobResponse>>
    {
        [JsonIgnore]
        public Guid JobId { get; init; }
    }
}