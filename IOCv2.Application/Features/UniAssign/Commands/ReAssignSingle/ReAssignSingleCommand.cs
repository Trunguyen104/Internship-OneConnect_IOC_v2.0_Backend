using System;
using MediatR;
using IOCv2.Application.Common.Models;

namespace IOCv2.Application.Features.UniAssign.Commands.ReAssignSingle
{
    public class ReAssignSingleCommand : IRequest<Result<ReAssignSingleResponse>>
    {
        public Guid StudentId { get; init; }
        public Guid NewEnterpriseId { get; init; }
        public Guid NewInternPhaseId { get; set; }
    }
}
