using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.StudentApplications.Commands.HideApplication;

public class HideApplicationHandler
    : IRequestHandler<HideApplicationCommand, Result<HideApplicationResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;

    public HideApplicationHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
    }

    public async Task<Result<HideApplicationResponse>> Handle(
        HideApplicationCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<HideApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

        var student = await _unitOfWork.Repository<Student>().Query()
            .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

        if (student == null)
            return Result<HideApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentMessageKey.StudentNotFound), ResultErrorType.NotFound);

        var application = await _unitOfWork.Repository<InternshipApplication>().Query()
            .FirstOrDefaultAsync(a => a.ApplicationId == request.ApplicationId, cancellationToken);

        if (application == null)
            return Result<HideApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentApplications.NotFound), ResultErrorType.NotFound);

        // Ownership check
        if (application.StudentId != student.StudentId)
            return Result<HideApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentApplications.NotOwner), ResultErrorType.Forbidden);

        // Business rule: cannot hide Placed (AC-04 Note)
        if (application.Status == InternshipApplicationStatus.Placed)
            return Result<HideApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentApplications.CannotHidePlaced), ResultErrorType.BadRequest);

        // Can only hide terminal states: Rejected or Withdrawn
        if (application.Status != InternshipApplicationStatus.Rejected &&
            application.Status != InternshipApplicationStatus.Withdrawn)
            return Result<HideApplicationResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentApplications.CannotHideActiveApplication), ResultErrorType.BadRequest);

        application.IsHiddenByStudent = true;
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<HideApplicationResponse>.Success(new HideApplicationResponse
        {
            ApplicationId = application.ApplicationId,
            Message = _messageService.GetMessage(MessageKeys.StudentApplications.HideSuccess)
        });
    }
}
