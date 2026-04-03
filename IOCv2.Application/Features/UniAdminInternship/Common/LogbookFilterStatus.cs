namespace IOCv2.Application.Features.UniAdminInternship.Common;

public enum LogbookFilterStatus
{
    Sufficient = 1,      // Missing = 0
    SlightlyMissing = 2, // 0 < Missing <= 3
    MissingMany = 3      // Missing > 3
}
