using System;
using MediatR;

namespace IOCv2.Application.Features.Notifications.Events
{
    public sealed record JobUpdatedEvent(Guid UserId, Guid JobId, string JobTitle, string EnterpriseName, string Message) : INotification;
}
