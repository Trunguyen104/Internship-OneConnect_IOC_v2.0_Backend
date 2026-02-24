namespace IOCv2.Application.Features.Sprints.Commands.StartSprint;

public class StartSprintRequest
{
    /// <summary>Ngày bắt đầu Sprint (bắt buộc)</summary>
    public DateOnly StartDate { get; set; }

    /// <summary>Ngày kết thúc Sprint (bắt buộc)</summary>
    public DateOnly EndDate { get; set; }
}
