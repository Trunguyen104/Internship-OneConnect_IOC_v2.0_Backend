using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Commands.ApproveUniAssign;

public class ApproveUniAssignHandler : IRequestHandler<ApproveUniAssignCommand, Result<ApproveUniAssignResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IPublisher _publisher;
    private readonly ILogger<ApproveUniAssignHandler> _logger;

    public ApproveUniAssignHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IPublisher publisher,
        ILogger<ApproveUniAssignHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<ApproveUniAssignResponse>> Handle(ApproveUniAssignCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(_messageService.GetMessage(MessageKeys.HRApplications.LogApproveUniAssign), request.ApplicationId);

        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<ApproveUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .Include(eu => eu.User)
                .Include(eu => eu.Enterprise)
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<ApproveUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.EnterpriseUserNotFound), ResultErrorType.Forbidden);

            var app = await _unitOfWork.Repository<InternshipApplication>().Query()
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Include(a => a.Enterprise)
                .Include(a => a.University)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == request.ApplicationId &&
                    a.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

            if (app == null)
                return Result<ApproveUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotFound), ResultErrorType.NotFound);

            if (app.Source != ApplicationSource.UniAssign)
                return Result<ApproveUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotUniAssignApplication), ResultErrorType.BadRequest);

            if (app.Status != InternshipApplicationStatus.PendingAssignment)
                return Result<ApproveUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.InvalidTransition), ResultErrorType.BadRequest);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // 1. Approve the Uni Assign application
            var approveHistory = new ApplicationStatusHistory
            {
                HistoryId = Guid.NewGuid(),
                ApplicationId = app.ApplicationId,
                FromStatus = app.Status,
                ToStatus = InternshipApplicationStatus.Placed,
                ChangedByName = enterpriseUser.User?.FullName ?? "HR",
                TriggerSource = "HR"
            };

            app.Status = InternshipApplicationStatus.Placed;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewedBy = enterpriseUser.EnterpriseUserId;

            if (app.Student != null)
                app.Student.InternshipStatus = StudentStatus.Placed;

            await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(approveHistory, cancellationToken);

            // 2. Cascade Withdraw: all other active applications of this student (both self-apply and uni-assign, any enterprise)
            var otherActiveApps = await _unitOfWork.Repository<InternshipApplication>().Query()
                .Include(a => a.Enterprise)
                .Where(a => a.StudentId == app.StudentId
                         && a.ApplicationId != app.ApplicationId
                         && (a.Status == InternshipApplicationStatus.Applied ||
                             a.Status == InternshipApplicationStatus.Interviewing ||
                             a.Status == InternshipApplicationStatus.Offered ||
                             a.Status == InternshipApplicationStatus.PendingAssignment))
                .ToListAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.HRApplications.LogCascadeWithdraw),
                app.StudentId, otherActiveApps.Count);

            var enterprisesToNotify = new HashSet<Guid>();
            foreach (var otherApp in otherActiveApps)
            {
                var withdrawHistory = new ApplicationStatusHistory
                {
                    HistoryId = Guid.NewGuid(),
                    ApplicationId = otherApp.ApplicationId,
                    FromStatus = otherApp.Status,
                    ToStatus = InternshipApplicationStatus.Withdrawn,
                    Note = "System-triggered: Student placed via Uni Assign",
                    ChangedByName = "System",
                    TriggerSource = "System"
                };
                otherApp.Status = InternshipApplicationStatus.Withdrawn;
                await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(withdrawHistory, cancellationToken);
                enterprisesToNotify.Add(otherApp.EnterpriseId);
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            // 3. Send notifications
            var studentUserId = app.Student?.UserId;
            var enterpriseName = app.Enterprise?.Name ?? string.Empty;

            if (studentUserId.HasValue)
            {
                await _publisher.Publish(new ApplicationPlacedUniAssignEvent(
                    studentUserId.Value,
                    app.ApplicationId,
                    enterpriseName), cancellationToken);
            }

            // Notify each affected enterprise's HR about the auto-withdraw
            foreach (var entId in enterprisesToNotify)
            {
                await _publisher.Publish(new ApplicationAutoWithdrawnNotifyEnterpriseEvent(
                    entId,
                    app.Student?.User?.FullName ?? string.Empty), cancellationToken);
            }

            return Result<ApproveUniAssignResponse>.Success(new ApproveUniAssignResponse
            {
                ApplicationId = app.ApplicationId,
                Status = app.Status,
                StatusLabel = app.Status.ToString(),
                WithdrawnApplicationsCount = otherActiveApps.Count,
                UpdatedAt = app.UpdatedAt ?? DateTime.UtcNow
            }, _messageService.GetMessage(MessageKeys.HRApplications.LogApproveUniAssign));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error approving uni-assign application {ApplicationId}", request.ApplicationId);
            return Result<ApproveUniAssignResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
        }
    }
}
