using MediatR;

namespace IOCv2.Application.Features.Notifications.Events;

/// <summary>Sinh viên được chuyển sang Interviewing.</summary>
public record ApplicationMovedToInterviewingEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string EnterpriseName
) : INotification;

/// <summary>Sinh viên nhận Offer.</summary>
public record ApplicationOfferedEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string EnterpriseName
) : INotification;

/// <summary>Sinh viên được Placed (Self-apply flow).</summary>
public record ApplicationPlacedSelfApplyEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string EnterpriseName
) : INotification;

/// <summary>Sinh viên bị Rejected (Self-apply flow).</summary>
public record ApplicationRejectedSelfApplyEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string EnterpriseName
) : INotification;

/// <summary>Sinh viên được Placed (Uni Assign flow) — notify student.</summary>
public record ApplicationPlacedUniAssignEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string EnterpriseName
) : INotification;

/// <summary>Sinh viên bị Rejected (Uni Assign flow) — notify student + Uni Admin.</summary>
public record ApplicationRejectedUniAssignEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string EnterpriseName,
    Guid? UniversityId,
    string StudentName,
    string RejectReason
) : INotification;

/// <summary>Enterprise liên quan thông báo tự động Withdrawn.</summary>
public record ApplicationAutoWithdrawnNotifyEnterpriseEvent(
    Guid EnterpriseId,
    string StudentName
) : INotification;

/// <summary>Uni Admin nhận thông báo khi sinh viên được Approve Uni Assign.</summary>
public record ApplicationApprovedNotifyUniAdminEvent(
    Guid? UniversityId,
    string StudentName,
    string EnterpriseName,
    Guid ApplicationId
) : INotification;

/// <summary>SV tự rút đơn — notify Enterprise HR (AC-07).</summary>
public record ApplicationWithdrawnByStudentEvent(
    Guid EnterpriseId,
    string StudentName,
    string JobTitle,
    Guid ApplicationId
) : INotification;
