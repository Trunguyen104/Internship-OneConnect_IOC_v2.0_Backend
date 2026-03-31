namespace IOCv2.Application.Features.UniAdminInternship.Common;

public enum LogbookFilterStatus
{
    Sufficient = 1,      // PercentComplete >= 75
    SlightlyMissing = 2, // 50 <= PercentComplete < 75
    MissingMany = 3      // PercentComplete < 50
}
