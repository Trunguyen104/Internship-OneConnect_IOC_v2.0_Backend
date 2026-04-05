using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.UniAssign.Commands.QuickEnterpriseAssignment
{
    public record QuickEnterpriseAssignmentCommand : IRequest<Result<QuickEnterpriseAssignmentResponse>>
    {
        public Guid StudentId { get; init; }
        public Guid EnterpriseId { get; init; }
        public Guid InternPhaseId { get; init; }
    }
}
