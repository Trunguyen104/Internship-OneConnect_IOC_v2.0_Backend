using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Commands.RejectUniAssign;

public class RejectUniAssignHandler : IRequestHandler<RejectUniAssignCommand, Result<RejectUniAssignResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IPublisher _publisher;
    private readonly ILogger<RejectUniAssignHandler> _logger;

    public RejectUniAssignHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IPublisher publisher,
        ILogger<RejectUniAssignHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<RejectUniAssignResponse>> Handle(RejectUniAssignCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(_messageService.GetMessage(MessageKeys.HRApplications.LogRejectUniAssign), request.ApplicationId);

        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<RejectUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .Include(eu => eu.User)
                .Include(eu => eu.Enterprise)
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<RejectUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.EnterpriseUserNotFound), ResultErrorType.Forbidden);

            var app = await _unitOfWork.Repository<InternshipApplication>().Query()
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Include(a => a.Enterprise)
                .Include(a => a.University)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == request.ApplicationId &&
                    a.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

            if (app == null)
                return Result<RejectUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotFound), ResultErrorType.NotFound);

            if (app.Source != ApplicationSource.UniAssign)
                return Result<RejectUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotUniAssignApplication), ResultErrorType.BadRequest);

            if (app.Status != InternshipApplicationStatus.PendingAssignment)
                return Result<RejectUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.InvalidTransition), ResultErrorType.BadRequest);

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

            // Student returns to Unplaced
            if (app.Student != null)
                app.Student.InternshipStatus = StudentStatus.Unplaced;

            await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(history, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var enterpriseName = app.Enterprise?.Name ?? string.Empty;
            var studentName = app.Student?.User?.FullName ?? string.Empty;

            // Notify student
            var studentUserId = app.Student?.UserId;
            if (studentUserId.HasValue || app.UniversityId.HasValue)
            {
                await _publisher.Publish(new ApplicationRejectedUniAssignEvent(
                    studentUserId ?? Guid.Empty, // 0 if missing, but we only have 1 event
                    app.ApplicationId,
                    enterpriseName,
                    app.UniversityId,
                    studentName,
                    request.RejectReason), cancellationToken);
            }

            return Result<RejectUniAssignResponse>.Success(new RejectUniAssignResponse
            {
                ApplicationId = app.ApplicationId,
                Status = app.Status,
                StatusLabel = app.Status.ToString(),
                RejectReason = app.RejectReason ?? string.Empty,
                UpdatedAt = app.UpdatedAt ?? DateTime.UtcNow
            }, _messageService.GetMessage(MessageKeys.HRApplications.LogRejectUniAssign));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting uni-assign application {ApplicationId}", request.ApplicationId);
            return Result<RejectUniAssignResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
        }
    }
}
