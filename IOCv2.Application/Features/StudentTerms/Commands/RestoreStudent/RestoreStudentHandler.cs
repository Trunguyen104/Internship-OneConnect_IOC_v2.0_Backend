using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Terms.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Commands.RestoreStudent;

public class RestoreStudentHandler : IRequestHandler<RestoreStudentCommand, Result<RestoreStudentResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly IBackgroundEmailSender _emailSender;
    private readonly ICacheService _cacheService;
    private readonly ILogger<RestoreStudentHandler> _logger;

    public RestoreStudentHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IBackgroundEmailSender emailSender,
        ICacheService cacheService,
        ILogger<RestoreStudentHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _emailSender = emailSender;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<RestoreStudentResponse>> Handle(RestoreStudentCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        var studentTerm = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Include(st => st.Term)
            .FirstOrDefaultAsync(st => st.StudentTermId == request.StudentTermId, cancellationToken);

        if (studentTerm == null)
            return Result<RestoreStudentResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.NotFound), ResultErrorType.NotFound);

        if (!isSuperAdmin)
        {
            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != studentTerm.Term.UniversityId)
                return Result<RestoreStudentResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
        }

        if (studentTerm.EnrollmentStatus != EnrollmentStatus.Withdrawn)
            return Result<RestoreStudentResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.NotWithdrawn));

        // Term must be Open and not ended
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (studentTerm.Term.Status != TermStatus.Open || studentTerm.Term.EndDate < today)
            return Result<RestoreStudentResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.TermNotOpen));

        // Restore
        studentTerm.EnrollmentStatus = EnrollmentStatus.Active;
        studentTerm.PlacementStatus = PlacementStatus.Unplaced;
        studentTerm.EnterpriseId = null;
        studentTerm.UpdatedBy = userId;
        studentTerm.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<StudentTerm>().UpdateAsync(studentTerm, cancellationToken);

        // Update counters
        var term = studentTerm.Term;
        term.TotalEnrolled++;
        term.TotalUnplaced++;
        await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);

        await _unitOfWork.SaveChangeAsync(cancellationToken);

        await _cacheService.RemoveByPatternAsync(TermCacheKeys.TermListPattern(), cancellationToken);
        await _cacheService.RemoveByPatternAsync(TermCacheKeys.TermDetailPattern(), cancellationToken);

        // Fire-and-forget email
        _ = _emailSender.EnqueueEmailAsync(
            studentTerm.Student.User.Email,
            _messageService.GetMessage(MessageKeys.StudentTerms.EmailSubjectRestore),
            _messageService.GetMessage(MessageKeys.StudentTerms.EmailBodyRestore, term.Name),
            studentTerm.StudentTermId,
            userId,
            CancellationToken.None);

        _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogRestored), request.StudentTermId, userId);

        return Result<RestoreStudentResponse>.Success(
            new RestoreStudentResponse { StudentTermId = request.StudentTermId },
            _messageService.GetMessage(MessageKeys.StudentTerms.RestoreSuccess));
    }
}
