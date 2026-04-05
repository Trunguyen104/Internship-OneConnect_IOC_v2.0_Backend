using IOCv2.Application.Common.Models;
using MediatR;
using System;
using System.Collections.Generic;

namespace IOCv2.Application.Features.UniAssign.Commands.BulkEnterpriseAssignment
{
    public record BulkEnterpriseAssignmentCommand : IRequest<Result<BulkEnterpriseAssignmentResponse>>
    {
        public Guid EnterpriseId { get; set; }
        public Guid InternPhaseId { get; set; }
        public List<Guid> StudentIds { get; set; } = new();
        public bool Force { get; set; } = false;
    }
}