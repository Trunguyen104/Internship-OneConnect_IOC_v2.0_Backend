namespace IOCv2.Application.Features.Sprints.Commands.CompleteSprint;

public class CompleteSprintRequest
{
    public MoveIncompleteItemsOption IncompleteItemsOption { get; set; }

    /// <summary>Chỉ truyền UUID nếu chọn chuyển sang Sprint có sẵn (ToExistingSprint)</summary>
    public Guid? TargetSprintId { get; set; }

    /// <summary>Chỉ truyền tên nếu chọn tạo Sprint mới (CreateNewSprint)</summary>
    public string? NewSprintName { get; set; }
}
