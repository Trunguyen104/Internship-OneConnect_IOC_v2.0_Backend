using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Commands.RestoreStudent;

public class RestoreStudentHandler : IRequestHandler<RestoreStudentCommand, Result<RestoreStudentResponse>>
{
    private readonly IBackgroundEmailSender _emailSender;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<RestoreStudentHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreStudentHandler(IUnitOfWork unitOfWork, IMessageService messageService,
        ILogger<RestoreStudentHandler> logger, ICurrentUserService currentUserService,
        IBackgroundEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
        _emailSender = emailSender;
    }

    public async Task<Result<RestoreStudentResponse>> Handle(
        RestoreStudentCommand request, CancellationToken cancellationToken)
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
                return Result<RestoreStudentResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.NotFound), ResultErrorType.NotFound);

            if (!isSuperAdmin)
            {
                var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
                    .Where(uu => uu.UserId == userId).FirstOrDefaultAsync(cancellationToken);
                if (universityUser == null || universityUser.UniversityId != studentTerm.Term.UniversityId)
                    return Result<RestoreStudentResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.AccessDenied), ResultErrorType.Forbidden);
            }

            if (studentTerm.EnrollmentStatus != EnrollmentStatus.Withdrawn)
                return Result<RestoreStudentResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.NotWithdrawn));

            // Prevent restoring into a closed or ended term
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (studentTerm.Term.Status != TermStatus.Open || studentTerm.Term.EndDate < today)
                return Result<RestoreStudentResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.TermEndedOrClosed));

            studentTerm.EnrollmentStatus = EnrollmentStatus.Active;
            studentTerm.PlacementStatus = PlacementStatus.Unplaced;
            studentTerm.EnterpriseId = null;

            studentTerm.Term.TotalEnrolled++;
            studentTerm.Term.TotalUnplaced++;
            await _unitOfWork.Repository<Term>().UpdateAsync(studentTerm.Term, cancellationToken);
            await _unitOfWork.Repository<StudentTerm>().UpdateAsync(studentTerm, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogRestored),
                request.StudentTermId, userId);

            // Fire-and-forget notification to student
            var studentEmail = studentTerm.Student?.User?.Email;
            var termName = studentTerm.Term?.Name;
            if (!string.IsNullOrEmpty(studentEmail) && !string.IsNullOrEmpty(termName))
            {
                var subject = _messageService.GetMessage(MessageKeys.StudentTerms.NotifyRestoreSubject);
                var body = string.Format(_messageService.GetMessage(MessageKeys.StudentTerms.NotifyRestoreBody), termName);
                await _emailSender.EnqueueEmailAsync(studentEmail, subject, body,
                    auditTargetId: request.StudentTermId, performedByEmployeeId: userId, cancellationToken: cancellationToken);
            }

            return Result<RestoreStudentResponse>.Success(
                new RestoreStudentResponse { StudentTermId = request.StudentTermId },
                _messageService.GetMessage(MessageKeys.StudentTerms.RestoreSuccess));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.StudentTerms.LogError));
            throw;
        }
    }
}
