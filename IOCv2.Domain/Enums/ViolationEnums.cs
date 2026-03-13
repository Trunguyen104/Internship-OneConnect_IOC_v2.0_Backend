using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Domain.Enums
{
    public enum ViolationStatus : short
    {
        Pending = 1,      // Chờ xử lý
        InProgress = 2,   // Đang xử lý
        Resolved = 3,     // Đã xử lý
        NoViolation = 4   // Không vi phạm
    }

    public enum ViolationType : short
    {
        Absence = 1,          // Vắng mặt
        LateSubmission = 2,   // Nộp muộn
        MisconductAtWork = 3, // Vi phạm quy tắc nơi làm việc
        PoorPerformance = 4,  // Hiệu suất kém
    }

    public enum ViolationSeverity : short
    {
        Low = 1,      // Nhẹ
        Medium = 2,   // Trung bình
        High = 3,     // Nghiêm trọng
        Critical = 4  // Rất nghiêm trọng
    }
}
