namespace IOCv2.Domain.Enums;

public enum NotificationType : short
{
    General = 0,
    ApplicationAccepted = 1,
    ApplicationRejected = 2,
    InternshipAssigned = 3,
    LogbookFeedback = 4,
    EvaluationPublished = 5,
    ApplicationStatusChanged = 6,
    SystemAlert = 7,
}
