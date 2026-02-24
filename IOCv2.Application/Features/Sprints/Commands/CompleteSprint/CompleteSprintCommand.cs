using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public record CompleteSprintCommand(
    Guid SprintId,
    MoveIncompleteItemsOption IncompleteItemsOption,
    Guid? TargetSprintId,   // Truyền UUID nếu chọn chuyển sang Sprint có sẵn
    string? NewSprintName   // Truyền tên nếu chọn tạo Sprint mới
) : IRequest<Result<CompleteSprintResponse>>;
