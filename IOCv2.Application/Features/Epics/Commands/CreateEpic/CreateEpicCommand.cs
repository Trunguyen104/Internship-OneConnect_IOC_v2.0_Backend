using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Epics.Commands.CreateEpic;

public record CreateEpicCommand : IRequest<Result<CreateEpicResponse>>
{
    public Guid ProjectId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
