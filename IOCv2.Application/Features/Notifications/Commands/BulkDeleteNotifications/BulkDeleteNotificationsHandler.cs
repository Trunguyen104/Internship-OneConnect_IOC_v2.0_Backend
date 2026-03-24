using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Notifications.Commands.BulkDeleteNotifications;

public class BulkDeleteNotificationsHandler : IRequestHandler<BulkDeleteNotificationsCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;

    public BulkDeleteNotificationsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
    }

    public async Task<Result<int>> Handle(BulkDeleteNotificationsCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Result<int>.Failure(MessageKeys.Common.Unauthorized, ResultErrorType.Unauthorized);
        }

        // Bulk update using EF Core ExecuteUpdateAsync
        var rowsDeleted = await _unitOfWork.Repository<Notification>()
            .Query()
            .Where(n => n.UserId == userId && request.Ids.Contains(n.NotificationId))
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.DeletedAt, DateTime.UtcNow), cancellationToken);

        return Result<int>.Success(rowsDeleted);
    }
}
