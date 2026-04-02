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
                .Include(a => a.Job)
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
                    _messageService.GetMessage(MessageKeys.HRApplications.NotUniAssignApplication));

            if (app.Status != InternshipApplicationStatus.PendingAssignment)
                return Result<ApproveUniAssignResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.InvalidTransition));

            // Capacity check: if job belongs to a phase with MaxStudents, ensure there is still room
            if (app.Job?.InternshipPhaseId.HasValue == true)
            {
                var phase = await _unitOfWork.Repository<InternshipPhase>().Query().AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PhaseId == app.Job.InternshipPhaseId!.Value, cancellationToken);

                if (phase != null)
                {
                    var placedCount = await _unitOfWork.Repository<InternshipApplication>().Query().AsNoTracking()
                        .CountAsync(a => a.Job != null
                            && a.Job.InternshipPhaseId == phase.PhaseId
                            && a.Status == InternshipApplicationStatus.Placed, cancellationToken);

                    if (placedCount >= phase.Capacity)
                        return Result<ApproveUniAssignResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.HRApplications.InternPhaseAtCapacity));
                }
            }

            // 1. Approve the Uni Assign application
            var approveHistory = new ApplicationStatusHistory
            {
                HistoryId = Guid.NewGuid(),
                ApplicationId = app.ApplicationId,
                FromStatus = app.Status,
                ToStatus = InternshipApplicationStatus.Placed,
                ChangedByName = enterpriseUser.User.FullName,
                TriggerSource = "HR"
            };

            app.Status = InternshipApplicationStatus.Placed;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewedBy = enterpriseUser.EnterpriseUserId;

            app.Student.InternshipStatus = StudentStatus.Placed;

            await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(approveHistory, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // 3. Send notifications
            var studentUserId = app.Student.UserId;
            var enterpriseName = app.Enterprise.Name;

            if (studentUserId != Guid.Empty)
            {
                await _publisher.Publish(new ApplicationPlacedUniAssignEvent(
                    studentUserId,
                    app.ApplicationId,
                    enterpriseName), cancellationToken);
            }

            // Notify Uni Admin about the approval
            if (app.UniversityId.HasValue)
            {
                await _publisher.Publish(new ApplicationApprovedNotifyUniAdminEvent(
                    app.UniversityId,
                    app.Student.User.FullName,
                    enterpriseName,
                    app.ApplicationId), cancellationToken);
            }

            return Result<ApproveUniAssignResponse>.Success(new ApproveUniAssignResponse
            {
                ApplicationId = app.ApplicationId,
                Status = app.Status,
                StatusLabel = app.Status.ToString(),
                WithdrawnApplicationsCount = 0,
                UpdatedAt = app.UpdatedAt ?? DateTime.UtcNow
            }, _messageService.GetMessage(MessageKeys.HRApplications.LogApproveUniAssign));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving uni-assign application {ApplicationId}", request.ApplicationId);
            return Result<ApproveUniAssignResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
        }
    }
}
