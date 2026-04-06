using MediatR;

namespace IOCv2.Application.Features.Notifications.Events;

/// <summary>AC-07: Student assigned by Uni Admin (pending confirmation).</summary>
public record ApplicationAssignedUniAssignEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string EnterpriseName,
    string TermName
) : INotification;

/// <summary>AC-07: Uni Admin reassign from PendingAssignment (enterprise updated before HR approve).</summary>
public record ApplicationReassignedFromPendingEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string NewEnterpriseName
) : INotification;

/// <summary>AC-07: Uni Admin reassign from Placed (old placed withdrawn + new pending created).</summary>
public record ApplicationReassignedFromPlacedEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string OldEnterpriseName,
    string NewEnterpriseName
) : INotification;

/// <summary>AC-07: Uni Admin unassign (withdraw) an application.</summary>
public record ApplicationUnassignedUniAssignEvent(
    Guid StudentUserId,
    Guid ApplicationId,
    string TermName
) : INotification;