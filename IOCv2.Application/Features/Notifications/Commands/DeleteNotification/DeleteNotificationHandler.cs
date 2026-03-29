using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Notifications.Commands.DeleteNotification;

public class DeleteNotificationHandler : IRequestHandler<DeleteNotificationCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public DeleteNotificationHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Result<bool>.Failure(MessageKeys.Common.Unauthorized, ResultErrorType.Unauthorized);
        }

        var notification = await _unitOfWork.Repository<Notification>()
            .Query()
            .FirstOrDefaultAsync(n => n.NotificationId == request.Id, cancellationToken);

        if (notification == null)
        {
            return Result<bool>.Failure(MessageKeys.Notifications.NotFound, ResultErrorType.NotFound);
        }

        if (notification.UserId != userId)
        {
            return Result<bool>.Failure(MessageKeys.Notifications.DeleteNotOwner, ResultErrorType.Forbidden);
        }

        // Soft delete
        notification.DeletedAt = DateTime.UtcNow;

        await _unitOfWork.Repository<Notification>().UpdateAsync(notification, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
