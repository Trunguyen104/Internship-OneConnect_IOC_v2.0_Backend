using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Commands.MarkAsPlaced;

public record MarkAsPlacedCommand(Guid ApplicationId) : IRequest<Result<MarkAsPlacedResponse>>;
