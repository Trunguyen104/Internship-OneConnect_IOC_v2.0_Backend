using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Notifications.Queries.GetUnreadCount;

public record GetUnreadCountQuery : IRequest<Result<GetUnreadCountResponse>>;
