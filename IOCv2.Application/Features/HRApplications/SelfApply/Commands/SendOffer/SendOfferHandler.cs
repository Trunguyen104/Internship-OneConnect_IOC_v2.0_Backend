using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.HRApplications.SelfApply.Commands.SendOffer;

public class SendOfferHandler : IRequestHandler<SendOfferCommand, Result<SendOfferResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IPublisher _publisher;
    private readonly ILogger<SendOfferHandler> _logger;

    public SendOfferHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IPublisher publisher,
        ILogger<SendOfferHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<SendOfferResponse>> Handle(SendOfferCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(_messageService.GetMessage(MessageKeys.HRApplications.LogSendOffer), request.ApplicationId);

        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<SendOfferResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .Include(eu => eu.User)
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<SendOfferResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.EnterpriseUserNotFound), ResultErrorType.Forbidden);

            var app = await _unitOfWork.Repository<InternshipApplication>().Query()
                .Include(a => a.Student).ThenInclude(s => s.User)
                .Include(a => a.Enterprise)
                .FirstOrDefaultAsync(a =>
                    a.ApplicationId == request.ApplicationId &&
                    a.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

            if (app == null)
                return Result<SendOfferResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotFound), ResultErrorType.NotFound);

            if (app.Source != ApplicationSource.SelfApply)
                return Result<SendOfferResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.NotSelfApplyApplication), ResultErrorType.BadRequest);

            if (app.Status != InternshipApplicationStatus.Interviewing)
                return Result<SendOfferResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.HRApplications.InvalidTransition), ResultErrorType.BadRequest);

            var history = new ApplicationStatusHistory
            {
                HistoryId = Guid.NewGuid(),
                ApplicationId = app.ApplicationId,
                FromStatus = app.Status,
                ToStatus = InternshipApplicationStatus.Offered,
                ChangedByName = enterpriseUser.User?.FullName ?? "HR",
                TriggerSource = "HR"
            };

            app.Status = InternshipApplicationStatus.Offered;
            app.ReviewedAt = DateTime.UtcNow;
            app.ReviewedBy = enterpriseUser.EnterpriseUserId;

            await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(history, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var studentUserId = app.Student?.UserId;
            if (studentUserId.HasValue)
            {
                await _publisher.Publish(new ApplicationOfferedEvent(
                    studentUserId.Value,
                    app.ApplicationId,
                    app.Enterprise?.Name ?? string.Empty), cancellationToken);
            }

            return Result<SendOfferResponse>.Success(new SendOfferResponse
            {
                ApplicationId = app.ApplicationId,
                Status = app.Status,
                StatusLabel = app.Status.ToString(),
                UpdatedAt = app.UpdatedAt ?? DateTime.UtcNow
            }, _messageService.GetMessage(MessageKeys.HRApplications.LogSendOffer));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending offer for application {ApplicationId}", request.ApplicationId);
            return Result<SendOfferResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
        }
    }
}
