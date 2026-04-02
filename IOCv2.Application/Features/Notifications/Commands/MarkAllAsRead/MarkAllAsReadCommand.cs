using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Notifications.Commands.MarkAllAsRead;

public record MarkAllAsReadCommand : IRequest<Result<MarkAllAsReadResponse>>;
