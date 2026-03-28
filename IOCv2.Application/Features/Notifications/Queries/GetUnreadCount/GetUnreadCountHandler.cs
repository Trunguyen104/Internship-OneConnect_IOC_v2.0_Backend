using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Notifications.Queries.GetUnreadCount;

public class GetUnreadCountHandler : IRequestHandler<GetUnreadCountQuery, Result<GetUnreadCountResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetUnreadCountHandler> _logger;

    public GetUnreadCountHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<GetUnreadCountHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<GetUnreadCountResponse>> Handle(
        GetUnreadCountQuery request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<GetUnreadCountResponse>.Failure("Unauthorized", ResultErrorType.Unauthorized);

        var count = await _unitOfWork.Repository<Notification>()
            .CountAsync(n => n.UserId == currentUserId && !n.IsRead, cancellationToken);

        _logger.LogInformation("User {UserId} has {Count} unread notifications", currentUserId, count);

        return Result<GetUnreadCountResponse>.Success(new GetUnreadCountResponse { UnreadCount = count });
    }
}
