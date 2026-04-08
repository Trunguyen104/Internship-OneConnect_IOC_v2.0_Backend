using System;
using MediatR;
using IOCv2.Application.Common.Models;

namespace IOCv2.Application.Features.UniAssign.Commands.UnAssignSingle
{
    public class UnAssignSingleCommand : IRequest<Result<UnAssignSingleResponse>>
    {
        public Guid StudentId { get; init; }
    }
}
