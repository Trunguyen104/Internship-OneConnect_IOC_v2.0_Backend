using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Commands.MoveToInterviewing;

public class MoveToInterviewingHandler : IRequestHandler<MoveToInterviewingCommand, Result<MoveToInterviewingResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IPublisher _publisher;
    private readonly ILogger<MoveToInterviewingHandler> _logger;

    public MoveToInterviewingHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IPublisher publisher,
        ILogger<MoveToInterviewingHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<MoveToInterviewingResponse>> Handle(
        MoveToInterviewingCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(_messageService.GetMessage(MessageKeys.HRApplications.LogMoveToInterviewing), request.ApplicationId);

        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<MoveToInterviewingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .Include(eu => eu.User)
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<MoveToInterviewingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.EnterpriseUserNotFound), ResultErrorType.Forbidden);

            var app = await _unitOfWork.Repository<InternshipApplication>().Query()
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Include(a => a.Enterprise)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == request.ApplicationId &&
                    a.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

            if (app == null)
                return Result<MoveToInterviewingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotFound), ResultErrorType.NotFound);

            if (app.Source != ApplicationSource.SelfApply)
                return Result<MoveToInterviewingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotSelfApplyApplication), ResultErrorType.BadRequest);

            if (app.Status != InternshipApplicationStatus.Applied)
                return Result<MoveToInterviewingResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.InvalidTransition), ResultErrorType.BadRequest);

            var history = new ApplicationStatusHistory
            {
                HistoryId = Guid.NewGuid(),
                ApplicationId = app.ApplicationId,
                FromStatus = app.Status,
                ToStatus = InternshipApplicationStatus.Interviewing,
                ChangedByName = enterpriseUser.User?.FullName ?? "HR",
                TriggerSource = "HR"
            };

            app.Status = InternshipApplicationStatus.Interviewing;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewedBy = enterpriseUser.EnterpriseUserId;

            await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(history, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Send in-app notification to student
            var studentUserId = app.Student?.UserId;
            if (studentUserId.HasValue)
            {
                await _publisher.Publish(new ApplicationMovedToInterviewingEvent(
                    studentUserId.Value,
                    app.ApplicationId,
                    app.Enterprise?.Name ?? string.Empty), cancellationToken);
            }

            return Result<MoveToInterviewingResponse>.Success(new MoveToInterviewingResponse
            {
                ApplicationId = app.ApplicationId,
                Status = app.Status,
                StatusLabel = app.Status.ToString(),
                UpdatedAt = app.UpdatedAt ?? DateTime.UtcNow
            }, _messageService.GetMessage(MessageKeys.HRApplications.LogMoveToInterviewing));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving application {ApplicationId} to Interviewing", request.ApplicationId);
            return Result<MoveToInterviewingResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
        }
    }
}
