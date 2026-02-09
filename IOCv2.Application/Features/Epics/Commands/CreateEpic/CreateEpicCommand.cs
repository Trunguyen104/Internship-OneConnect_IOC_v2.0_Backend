using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Epics.Commands.CreateEpic;

public record CreateEpicCommand(
    Guid ProjectId,
    string Name,
    string? Description
) : IRequest<Result<CreateEpicResponse>>;
