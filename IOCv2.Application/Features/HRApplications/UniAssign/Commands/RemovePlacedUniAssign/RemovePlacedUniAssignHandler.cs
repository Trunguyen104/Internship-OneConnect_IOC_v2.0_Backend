using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.HRApplications.UniAssign.Commands.RemovePlacedUniAssign;

public class RemovePlacedUniAssignHandler
    : IRequestHandler<RemovePlacedUniAssignCommand, Result<RemovePlacedUniAssignResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IPublisher _publisher;
    private readonly ILogger<RemovePlacedUniAssignHandler> _logger;

    public RemovePlacedUniAssignHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IPublisher publisher,
        ILogger<RemovePlacedUniAssignHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<Result<RemovePlacedUniAssignResponse>> Handle(
        RemovePlacedUniAssignCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            _logger.LogWarning(_messageService.GetMessage(MessageKeys.HRApplications.LogInvalidUserId));
            return Result<RemovePlacedUniAssignResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
        }

        var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
            .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

        if (enterpriseUser == null)
            return Result<RemovePlacedUniAssignResponse>.Failure(
                _messageService.GetMessage(MessageKeys.HRApplications.EnterpriseUserNotFound), ResultErrorType.Forbidden);

        var app = await _unitOfWork.Repository<InternshipApplication>().Query()
            .Include(a => a.Student).ThenInclude(s => s.User)
            .Include(a => a.University)
            .Include(a => a.Job)
            .FirstOrDefaultAsync(a =>
                a.ApplicationId == request.ApplicationId &&
                a.EnterpriseId == enterpriseUser.EnterpriseId, cancellationToken);

        if (app == null)
            return Result<RemovePlacedUniAssignResponse>.Failure(
                _messageService.GetMessage(MessageKeys.HRApplications.NotFound), ResultErrorType.NotFound);

        if (app.Source != ApplicationSource.UniAssign)
            return Result<RemovePlacedUniAssignResponse>.Failure(
                _messageService.GetMessage(MessageKeys.HRApplications.NotUniAssignApplication), ResultErrorType.BadRequest);

        if (app.Status != InternshipApplicationStatus.Placed)
            return Result<RemovePlacedUniAssignResponse>.Failure(
                _messageService.GetMessage(MessageKeys.HRApplications.ApplicationNotPlaced), ResultErrorType.BadRequest);

        var previousStatus = app.Status;
        var enterpriseName = app.Enterprise?.Name ?? string.Empty;
        var studentUserId = app.Student?.UserId ?? Guid.Empty;
        var studentName = app.Student?.User?.FullName ?? string.Empty;
        var universityId = app.UniversityId;

        var currentUser = await _unitOfWork.Repository<User>().Query().AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == currentUserId, cancellationToken);

        // 1. Status history — ghi nhận action "Remove Placed" phân biệt với Reject thông thường
        var historyEntry = new ApplicationStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            ApplicationId = app.ApplicationId,
            FromStatus = previousStatus,
            ToStatus = InternshipApplicationStatus.Rejected,
            Note = "HR removed student from placed list (AC-C05)",
            TriggerSource = "HR-Remove",
            ChangedByName = currentUser?.FullName ?? currentUser?.Email ?? "HR",
            CreatedAt = DateTime.UtcNow
        };
        await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(historyEntry, cancellationToken);

        // 2. Update application status
        app.Status = InternshipApplicationStatus.Rejected;

        // 3. Reset student internship status
        if (app.Student != null)
            app.Student.InternshipStatus = StudentStatus.Unplaced;

        await _unitOfWork.Repository<InternshipApplication>().UpdateAsync(app, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.HRApplications.LogRemovePlacedUniAssign),
            app.ApplicationId, currentUserId);

        // 4. Publish notification event
        await _publisher.Publish(new PlacedStudentRemovedEvent(
            StudentUserId: studentUserId,
            ApplicationId: app.ApplicationId,
            EnterpriseName: enterpriseName,
            UniversityId: universityId,
            StudentName: studentName),
            cancellationToken);

        return Result<RemovePlacedUniAssignResponse>.Success(new RemovePlacedUniAssignResponse
        {
            ApplicationId = app.ApplicationId,
            Status = app.Status,
            StatusLabel = app.Status.ToString(),
            UpdatedAt = DateTime.UtcNow,
            Message = $"Student {studentName} has been removed from the placed list."
        });
    }
}
