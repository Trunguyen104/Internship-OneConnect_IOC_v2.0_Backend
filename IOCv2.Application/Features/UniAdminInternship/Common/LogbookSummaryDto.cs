namespace IOCv2.Application.Features.UniAdminInternship.Common;

public class LogbookSummaryDto
{
    public int Missing { get; set; }         // Z = Y - X (số ngày thiếu logbook)
    public int Submitted { get; set; }       // X = số logbook đã nộp
    public int Total { get; set; }           // Y = số ngày làm việc (T2-T6)
    public int PercentComplete { get; set; } // X/Y*100
}
