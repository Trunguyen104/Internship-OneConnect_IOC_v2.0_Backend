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
    }
}