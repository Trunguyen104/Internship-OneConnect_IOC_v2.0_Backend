using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Commands.RejectApplication;

public record RejectApplicationCommand : IRequest<Result<RejectApplicationResponse>>
{
    public Guid ApplicationId { get; init; }
    public string RejectReason { get; init; } = null!;
}
