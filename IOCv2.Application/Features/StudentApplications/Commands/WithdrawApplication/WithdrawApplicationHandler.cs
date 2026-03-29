using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.StudentApplications.Commands.WithdrawApplication;

public class WithdrawApplicationHandler
    : IRequestHandler<WithdrawApplicationCommand, Result<WithdrawApplicationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IPublisher _publisher;

    public WithdrawApplicationHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IPublisher publisher)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _publisher = publisher;
    }

    public async Task<Result<WithdrawApplicationResponse>> Handle(
        WithdrawApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<WithdrawApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

        // Resolve Student from UserId
        var student = await _unitOfWork.Repository<Student>().Query()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

        if (student == null)
            return Result<WithdrawApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentMessageKey.StudentNotFound), ResultErrorType.NotFound);

        // Load the application with enterprise + job info for notification
        var application = await _unitOfWork.Repository<InternshipApplication>().Query()
            .Include(a => a.Enterprise)
            .Include(a => a.Job)
            .Include(a => a.Student).ThenInclude(s => s.User)
            .FirstOrDefaultAsync(a => a.ApplicationId == request.ApplicationId, cancellationToken);

        if (application == null)
            return Result<WithdrawApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentApplications.NotFound), ResultErrorType.NotFound);

        // Ownership check
        if (application.StudentId != student.StudentId)
            return Result<WithdrawApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentApplications.NotOwner), ResultErrorType.Forbidden);

        // Business rule: only Applied can be withdrawn (AC-02)
        if (application.Status != InternshipApplicationStatus.Applied)
            return Result<WithdrawApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentApplications.CannotWithdrawNotApplied), ResultErrorType.BadRequest);

        // Update status
        application.Status = InternshipApplicationStatus.Withdrawn;

        // Update student placement status -> Unplaced
        student.InternshipStatus = StudentStatus.Unplaced;

        // Log status history
        await _unitOfWork.Repository<ApplicationStatusHistory>().AddAsync(new ApplicationStatusHistory
        {
            HistoryId = Guid.NewGuid(),
            ApplicationId = application.ApplicationId,
            FromStatus = InternshipApplicationStatus.Applied,
            ToStatus = InternshipApplicationStatus.Withdrawn,
            TriggerSource = "Student",
            ChangedByName = application.Student.User.FullName,
            Note = "Sinh viên tự rút đơn.",
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangeAsync(cancellationToken);

        // Notify HR (AC-07)
        var studentName = application.Student.User.FullName;
        var jobTitle = application.Job?.Title ?? "(chưa rõ vị trí)";
        await _publisher.Publish(new ApplicationWithdrawnByStudentEvent(
            application.EnterpriseId, studentName, jobTitle, application.ApplicationId), cancellationToken);

        return Result<WithdrawApplicationResponse>.Success(new WithdrawApplicationResponse
        {
            ApplicationId = application.ApplicationId,
            Message = _messageService.GetMessage(MessageKeys.StudentApplications.WithdrawSuccess)
        });
    }
}
