using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Notifications.Commands.MarkAsRead;

public class MarkAsReadHandler : IRequestHandler<MarkAsReadCommand, Result<MarkAsReadResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<MarkAsReadHandler> _logger;

    public MarkAsReadHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<MarkAsReadHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<MarkAsReadResponse>> Handle(
        MarkAsReadCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<MarkAsReadResponse>.Failure("Unauthorized", ResultErrorType.Unauthorized);

        var notification = await _unitOfWork.Repository<Notification>()
            .Query()
            .FirstOrDefaultAsync(n => n.NotificationId == request.NotificationId, cancellationToken);

        if (notification is null)
            return Result<MarkAsReadResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Notifications.NotFound), ResultErrorType.NotFound);

        // Ownership check — user chỉ được đọc thông báo của chính mình
        if (notification.UserId != currentUserId)
            return Result<MarkAsReadResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Notifications.AccessDenied), ResultErrorType.Forbidden);

        if (notification.IsRead)
        {
            // Idempotent — đã đọc rồi thì return success luôn
            return Result<MarkAsReadResponse>.Success(new MarkAsReadResponse
            {
                NotificationId = notification.NotificationId,
                IsRead = notification.IsRead,
                ReadAt = notification.ReadAt
            });
        }

        notification.IsRead = true;
        notification.ReadAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Notification>().UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        _logger.LogInformation("User {UserId} marked notification {NotificationId} as read",
            currentUserId, request.NotificationId);

        return Result<MarkAsReadResponse>.Success(new MarkAsReadResponse
        {
            NotificationId = notification.NotificationId,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt
        }, _messageService.GetMessage(MessageKeys.Notifications.MarkReadSuccess));
    }
}
