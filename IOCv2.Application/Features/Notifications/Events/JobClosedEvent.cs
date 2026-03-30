using MediatR;
using System;

namespace IOCv2.Application.Features.Notifications.Events
{
    public sealed record JobClosedEvent(Guid UserId, Guid JobId, string JobTitle, string EnterpriseName) : INotification;
}
