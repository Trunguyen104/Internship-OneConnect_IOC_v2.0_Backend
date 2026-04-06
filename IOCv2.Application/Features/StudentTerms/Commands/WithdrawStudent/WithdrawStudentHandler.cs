using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Terms.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Commands.WithdrawStudent;

public class WithdrawStudentHandler : IRequestHandler<WithdrawStudentCommand, Result<WithdrawStudentResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<WithdrawStudentHandler> _logger;

    public WithdrawStudentHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ICacheService cacheService,
        ILogger<WithdrawStudentHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<WithdrawStudentResponse>> Handle(WithdrawStudentCommand request, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            return Result<WithdrawStudentResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

        var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        var studentTerm = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Include(st => st.Term)
            .FirstOrDefaultAsync(st => st.StudentTermId == request.StudentTermId, cancellationToken);

        if (studentTerm == null)
            return Result<WithdrawStudentResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.NotFound), ResultErrorType.NotFound);

        if (!isSuperAdmin)
        {
            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != studentTerm.Term.UniversityId)
                return Result<WithdrawStudentResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
        }

        var term = studentTerm.Term;
        if (TermStatusHelper.IsEnded(term.StartDate, term.EndDate, term.Status) ||
            TermStatusHelper.IsClosed(term.Status))
            return Result<WithdrawStudentResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.TermEndedOrClosed));

        if (studentTerm.EnrollmentStatus == EnrollmentStatus.Withdrawn)
            return Result<WithdrawStudentResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.AlreadyWithdrawn));

        if (studentTerm.PlacementStatus == PlacementStatus.Placed)
            throw new DomainViolationException(
                _messageService.GetMessage(MessageKeys.StudentTerms.CannotWithdrawPlaced));

        var hasOtherStudentTerms = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .AnyAsync(st => st.StudentId == studentTerm.StudentId && st.StudentTermId != studentTerm.StudentTermId, cancellationToken);

        if (hasOtherStudentTerms)
        {
            return Result<WithdrawStudentResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.CannotDeleteFromSystemHasOtherTerms));
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            studentTerm.EnrollmentStatus = EnrollmentStatus.Withdrawn;
            studentTerm.UpdatedBy = userId;
            studentTerm.UpdatedAt = DateTime.UtcNow;

            var now = DateTime.UtcNow;

            var user = studentTerm.Student.User;

            var activeTokens = await _unitOfWork.Repository<RefreshToken>()
                .Query()
                .Where(rt => rt.UserId == user.UserId && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = now;
                await _unitOfWork.Repository<RefreshToken>().UpdateAsync(token, cancellationToken);
            }

            studentTerm.DeletedBy = userId;
            await _unitOfWork.Repository<StudentTerm>().HardDeleteAsync(studentTerm, cancellationToken);
            await _unitOfWork.Repository<Student>().HardDeleteAsync(studentTerm.Student, cancellationToken);
            await _unitOfWork.Repository<User>().HardDeleteAsync(user, cancellationToken);

            term.TotalEnrolled = Math.Max(0, term.TotalEnrolled - 1);
            term.TotalUnplaced = Math.Max(0, term.TotalUnplaced - 1);
            await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        await _cacheService.RemoveByPatternAsync(TermCacheKeys.TermListPattern(), cancellationToken);
        await _cacheService.RemoveByPatternAsync(TermCacheKeys.TermDetailPattern(), cancellationToken);

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.StudentTerms.LogWithdrawn),
            request.StudentTermId,
            userId);

        return Result<WithdrawStudentResponse>.Success(
            new WithdrawStudentResponse
            {
                StudentTermId = request.StudentTermId,
                StudentDeletedFromSystem = true,
                SystemStudentDelta = -1,
                UiWarningMessageKey = MessageKeys.StudentTerms.WithdrawDeleteWarning,
                UiWarningMessage = _messageService.GetMessage(MessageKeys.StudentTerms.WithdrawDeleteWarning, -1)
            },
            _messageService.GetMessage(MessageKeys.StudentTerms.WithdrawSuccess));
    }
}
