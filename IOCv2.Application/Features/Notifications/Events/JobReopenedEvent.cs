using MediatR;

namespace IOCv2.Application.Features.Notifications.Events
{
    public sealed record JobReopenedEvent(Guid UserId, Guid JobId, string JobTitle, string EnterpriseName, string Message) : INotification;
}