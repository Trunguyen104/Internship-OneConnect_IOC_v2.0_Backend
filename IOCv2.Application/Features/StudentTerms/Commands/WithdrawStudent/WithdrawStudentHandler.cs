using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Commands.WithdrawStudent;

public class WithdrawStudentHandler : IRequestHandler<WithdrawStudentCommand, Result<WithdrawStudentResponse>>
{
    private readonly IBackgroundEmailSender _emailSender;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WithdrawStudentHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;

    public WithdrawStudentHandler(IUnitOfWork unitOfWork, IMessageService messageService,
        ILogger<WithdrawStudentHandler> logger, ICurrentUserService currentUserService,
        IBackgroundEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
        _emailSender = emailSender;
    }

    public async Task<Result<WithdrawStudentResponse>> Handle(
        WithdrawStudentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            var studentTerm = await _unitOfWork.Repository<StudentTerm>().Query()
                .Include(st => st.Term)
                .Include(st => st.Student).ThenInclude(s => s.User)
                .FirstOrDefaultAsync(st => st.StudentTermId == request.StudentTermId, cancellationToken);

            if (studentTerm == null)
                return Result<WithdrawStudentResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.NotFound), ResultErrorType.NotFound);

            if (!isSuperAdmin)
            {
                var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
                    .Where(uu => uu.UserId == userId).FirstOrDefaultAsync(cancellationToken);
                if (universityUser == null || universityUser.UniversityId != studentTerm.Term.UniversityId)
                    return Result<WithdrawStudentResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.AccessDenied), ResultErrorType.Forbidden);
            }

            if (studentTerm.EnrollmentStatus == EnrollmentStatus.Withdrawn)
                return Result<WithdrawStudentResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.AlreadyWithdrawn));

            if (studentTerm.PlacementStatus == PlacementStatus.Placed)
                return Result<WithdrawStudentResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.WithdrawFailedPlaced));

            studentTerm.EnrollmentStatus = EnrollmentStatus.Withdrawn;

            // Update term counter
            studentTerm.Term.TotalEnrolled = Math.Max(0, studentTerm.Term.TotalEnrolled - 1);
            studentTerm.Term.TotalUnplaced = Math.Max(0, studentTerm.Term.TotalUnplaced - 1);
            await _unitOfWork.Repository<Term>().UpdateAsync(studentTerm.Term, cancellationToken);
            await _unitOfWork.Repository<StudentTerm>().UpdateAsync(studentTerm, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogWithdrawn),
                request.StudentTermId, userId);

            // Fire-and-forget notification to student
            var studentEmail = studentTerm.Student?.User?.Email;
            var termName = studentTerm.Term?.Name;
            if (!string.IsNullOrEmpty(studentEmail) && !string.IsNullOrEmpty(termName))
            {
                var subject = _messageService.GetMessage(MessageKeys.StudentTerms.NotifyWithdrawSubject);
                var body = string.Format(_messageService.GetMessage(MessageKeys.StudentTerms.NotifyWithdrawBody), termName);
                await _emailSender.EnqueueEmailAsync(studentEmail, subject, body,
                    auditTargetId: request.StudentTermId, performedByEmployeeId: userId, cancellationToken: cancellationToken);
            }

            return Result<WithdrawStudentResponse>.Success(
                new WithdrawStudentResponse { StudentTermId = request.StudentTermId },
                _messageService.GetMessage(MessageKeys.StudentTerms.WithdrawSuccess));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.StudentTerms.LogError));
            throw;
        }
    }
}
