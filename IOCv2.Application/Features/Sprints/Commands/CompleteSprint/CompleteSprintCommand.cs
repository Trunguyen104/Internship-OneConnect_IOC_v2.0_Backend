using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public record CompleteSprintCommand : IRequest<Result<CompleteSprintResponse>>
{
    public Guid SprintId { get; init; }
    
    /// <summary>FE gửi string: "ToBacklog" | "ToNextPlannedSprint" | "CreateNewSprint"</summary>
    public string IncompleteItemsOption { get; init; } = string.Empty;

    /// <summary>Chỉ truyền UUID nếu chọn ToNextPlannedSprint + muốn chỉ định sprint cụ thể</summary>
    public Guid? TargetSprintId { get; init; }

    /// <summary>Chỉ truyền nếu chọn CreateNewSprint + muốn đặt tên tùy chỉnh</summary>
    public string? NewSprintName { get; init; }
}
