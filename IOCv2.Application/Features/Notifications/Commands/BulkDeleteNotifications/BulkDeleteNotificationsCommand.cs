using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Notifications.Commands.BulkDeleteNotifications;

public record BulkDeleteNotificationsCommand(List<Guid> Ids) : IRequest<Result<int>>;
