namespace IOCv2.Domain.Enums;

public enum InternshipApplicationStatus : short
{
    // ── Self-apply flow ──────────────────────────────────
    /// <summary>SV vừa nộp đơn, chờ HR xem xét.</summary>
    Applied = 1,
    
    /// <summary>Đang trong quá trình phỏng vấn.</summary>
    Interviewing = 2,

    /// <summary>HR đã gửi offer, chờ kết quả.</summary>
    Offered = 3,

    // ── Uni Assign flow ──────────────────────────────────
    /// <summary>Trường đã chỉ định, chờ HR duyệt.</summary>
    PendingAssignment = 4,

    // ── Terminal states (cả 2 flow) ──────────────────────
    /// <summary>SV chính thức được nhận thực tập.</summary>
    Placed = 5,

    /// <summary>HR từ chối đơn.</summary>
    Rejected = 6,

    /// <summary>SV tự rút hoặc system-triggered.</summary>
    Withdrawn = 7,
}
