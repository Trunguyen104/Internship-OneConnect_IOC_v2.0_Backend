namespace IOCv2.Application.Features.UniAdminInternship.Common;

public enum InternshipUiStatus
{
    Active = 1,               // Đang thực tập (Placed + có group + term Open)
    NoGroup = 2,              // Chưa có nhóm (Placed + không có group)
    Completed = 3,            // Hoàn tất (term Closed)
    PendingConfirmation = 4,  // Chờ xác nhận (Unplaced + có active application)
    Unplaced = 5              // Chưa có nơi thực tập
}
