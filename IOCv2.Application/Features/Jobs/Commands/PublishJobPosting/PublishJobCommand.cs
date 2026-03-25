using IOCv2.Application.Common.Models;
using MediatR;
using System;

namespace IOCv2.Application.Features.Jobs.Commands.PublishJob
{
    public record PublishJobCommand : IRequest<Result<PublishJobResponse>>
    {
        public Guid JobId { get; init; }
    }
}