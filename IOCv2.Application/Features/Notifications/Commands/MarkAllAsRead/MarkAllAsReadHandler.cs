using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace IOCv2.Application.Features.Notifications.Commands.MarkAllAsRead;

public class MarkAllAsReadHandler : IRequestHandler<MarkAllAsReadCommand, Result<MarkAllAsReadResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<MarkAllAsReadHandler> _logger;

    public MarkAllAsReadHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<MarkAllAsReadHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<MarkAllAsReadResponse>> Handle(
        MarkAllAsReadCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<MarkAllAsReadResponse>.Failure("Unauthorized", ResultErrorType.Unauthorized);

        var now = DateTime.UtcNow;

        // Bulk UPDATE — sinh 1 câu SQL duy nhất, không load entity vào memory
        var updatedCount = await _unitOfWork.Repository<Notification>().ExecuteUpdateAsync(
            n => n.UserId == currentUserId && !n.IsRead,
            s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now),
            cancellationToken);

        _logger.LogInformation("User {UserId} marked all {Count} notifications as read", currentUserId, updatedCount);

        return Result<MarkAllAsReadResponse>.Success(
            new MarkAllAsReadResponse { UpdatedCount = updatedCount },
            _messageService.GetMessage(MessageKeys.Notifications.MarkAllReadSuccess));
    }
}
