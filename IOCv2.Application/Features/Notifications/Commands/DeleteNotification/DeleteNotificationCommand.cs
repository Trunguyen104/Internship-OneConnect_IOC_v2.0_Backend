using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Notifications.Commands.DeleteNotification;

public record DeleteNotificationCommand(Guid Id) : IRequest<Result<bool>>;
