using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Commands.RejectUniAssign;

public record RejectUniAssignCommand : IRequest<Result<RejectUniAssignResponse>>
{
    public Guid ApplicationId { get; init; }
    public string RejectReason { get; init; } = null!;
}
