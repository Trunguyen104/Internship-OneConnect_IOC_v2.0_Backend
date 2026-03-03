using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Epics.Commands.UpdateEpic;

public record UpdateEpicCommand : IRequest<Result<UpdateEpicResponse>>
{
    [JsonIgnore]
    public Guid ProjectId { get; init; }

    [JsonIgnore]
    public Guid EpicId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
