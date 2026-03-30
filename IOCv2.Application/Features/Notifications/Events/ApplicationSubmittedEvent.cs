using MediatR;
using System;

namespace IOCv2.Application.Features.Notifications.Events
{
    public sealed record ApplicationSubmittedEvent(
        Guid RecipientUserId,
        Guid ApplicationId,
        Guid JobId,
        string StudentName,
        string JobTitle,
        string EnterpriseName,
        string Message) : INotification;
}
