using System.Text.Json.Serialization;
using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.WorkItems.Commands.DeleteWorkItem;

public record DeleteWorkItemCommand : IRequest<Result<DeleteWorkItemResponse>>
{
    public Guid ProjectId { get; init; }

    [JsonIgnore]
    public Guid WorkItemId { get; init; }
}
