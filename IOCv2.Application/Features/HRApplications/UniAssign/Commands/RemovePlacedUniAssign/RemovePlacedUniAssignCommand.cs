using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Commands.RemovePlacedUniAssign;

public record RemovePlacedUniAssignCommand : IRequest<Result<RemovePlacedUniAssignResponse>>
{
    public Guid ApplicationId { get; init; }
}
