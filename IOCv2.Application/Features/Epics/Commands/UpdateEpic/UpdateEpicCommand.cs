using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Epics.Commands.UpdateEpic;

public record UpdateEpicCommand(
    Guid EpicId,
    string Name,
    string? Description
) : IRequest<Result<UpdateEpicResponse>>;
