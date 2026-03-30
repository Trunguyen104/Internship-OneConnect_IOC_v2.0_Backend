using IOCv2.Application.Common.Models;
using MediatR;
using System;

namespace IOCv2.Application.Features.Jobs.Commands.ApplyJob
{
    public record ApplyJobCommand(Guid JobId) : IRequest<Result<ApplyJobResponse>>;
}
