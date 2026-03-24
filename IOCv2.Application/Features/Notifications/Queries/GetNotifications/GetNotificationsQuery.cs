using IOCv2.Application.Common.Models;
using MediatR;

namespace IOCv2.Application.Features.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery : IRequest<Result<PaginatedResult<GetNotificationsResponse>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
