using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Commands.RejectApplication;

public class RejectApplicationHandler : IRequestHandler<RejectApplicationCommand, Result<RejectApplicationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IPublisher _publisher;
    private readonly ILogger<RejectApplicationHandler> _logger;

    public RejectApplicationHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IPublisher publisher,
        ILogger<RejectApplicationHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<RejectApplicationResponse>> Handle(
        RejectApplicationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(_messageService.GetMessage(MessageKeys.HRApplications.LogReject), request.ApplicationId);

        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<RejectApplicationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .Include(eu => eu.User)
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<RejectApplicationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.EnterpriseUserNotFound), ResultErrorType.Forbidden);

            var app = await _unitOfWork.Repository<InternshipApplication>().Query()
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Include(a => a.Enterprise)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == request.ApplicationId &&
                    a.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

            if (app == null)
                return Result<RejectApplicationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotFound), ResultErrorType.NotFound);

            if (app.Source != ApplicationSource.SelfApply)
                return Result<RejectApplicationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotSelfApplyApplication), ResultErrorType.BadRequest);

            // Cannot reject a Placed application
            if (app.Status == InternshipApplicationStatus.Placed)
                return Result<RejectApplicationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.CannotRejectPlaced), ResultErrorType.BadRequest);

            // Must be an active stage
            if (!app.IsActive())
                return Result<RejectApplicationResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.ApplicationNotActive), ResultErrorType.BadRequest);

            var history = new ApplicationStatusHistory
            {
                HistoryId = Guid.NewGuid(),
                ApplicationId = app.ApplicationId,
                FromStatus = app.Status,
                ToStatus = InternshipApplicationStatus.Rejected,
                Note = request.RejectReason,
                ChangedByName = enterpriseUser.User?.FullName ?? "HR",
                TriggerSource = "HR"
            };

            app.Status = InternshipApplicationStatus.Rejected;
            app.RejectReason = request.RejectReason;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewedBy = enterpriseUser.EnterpriseUserId;

            // Student returns to Unplaced so they can apply elsewhere
            if (app.Student != null)
                app.Student.InternshipStatus = StudentStatus.Unplaced;

            await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(history, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var studentUserId = app.Student?.UserId;
            if (studentUserId.HasValue)
            {
                await _publisher.Publish(new ApplicationRejectedSelfApplyEvent(
                    studentUserId.Value,
                    app.ApplicationId,
                    app.Enterprise?.Name ?? string.Empty), cancellationToken);
            }

            return Result<RejectApplicationResponse>.Success(new RejectApplicationResponse
            {
                ApplicationId = app.ApplicationId,
                Status = app.Status,
                StatusLabel = app.Status.ToString(),
                RejectReason = app.RejectReason ?? string.Empty,
                UpdatedAt = app.UpdatedAt ?? DateTime.UtcNow
            }, _messageService.GetMessage(MessageKeys.HRApplications.LogReject));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting application {ApplicationId}", request.ApplicationId);
            return Result<RejectApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
        }
    }
}
