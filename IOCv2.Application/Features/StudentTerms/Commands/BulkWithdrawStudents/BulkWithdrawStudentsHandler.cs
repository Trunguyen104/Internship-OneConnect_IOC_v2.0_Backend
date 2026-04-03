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

namespace IOCv2.Application.Features.StudentTerms.Commands.BulkWithdrawStudents;

public class BulkWithdrawStudentsHandler : IRequestHandler<BulkWithdrawStudentsCommand, Result<BulkWithdrawStudentsResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<BulkWithdrawStudentsHandler> _logger;

    public BulkWithdrawStudentsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ICacheService cacheService,
        ILogger<BulkWithdrawStudentsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<BulkWithdrawStudentsResponse>> Handle(BulkWithdrawStudentsCommand request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        var term = await _unitOfWork.Repository<Term>()
            .Query()
            .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

        if (term == null)
            return Result<BulkWithdrawStudentsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Terms.NotFound), ResultErrorType.NotFound);

        if (!isSuperAdmin)
        {
            var universityUser = await _unitOfWork.Repository<UniversityUser>()
                .Query()
                .FirstOrDefaultAsync(uu => uu.UserId == userId, cancellationToken);

            if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                return Result<BulkWithdrawStudentsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
        }

        if (TermStatusHelper.IsEnded(term.StartDate, term.EndDate, term.Status) ||
            TermStatusHelper.IsClosed(term.Status))
            return Result<BulkWithdrawStudentsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.TermEndedOrClosed));

        var studentTerms = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Where(st => request.StudentTermIds.Contains(st.StudentTermId) && st.TermId == request.TermId)
            .ToListAsync(cancellationToken);

        var foundIds = studentTerms.Select(st => st.StudentTermId).ToHashSet();
        var notFoundIds = request.StudentTermIds.Where(id => !foundIds.Contains(id)).ToList();
        if (notFoundIds.Count > 0)
            return Result<BulkWithdrawStudentsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.NotFound), ResultErrorType.NotFound);

        var skippedPlaced = studentTerms.Count(st => st.PlacementStatus == PlacementStatus.Placed);
        var skippedWithdrawn = studentTerms.Count(st => st.EnrollmentStatus == EnrollmentStatus.Withdrawn);

        var candidates = studentTerms
            .Where(st => st.EnrollmentStatus == EnrollmentStatus.Active && st.PlacementStatus == PlacementStatus.Unplaced)
            .ToList();

        var candidateStudentIds = candidates.Select(st => st.StudentId).Distinct().ToList();
        var candidateStudentTermIds = candidates.Select(st => st.StudentTermId).ToList();

        var blockedStudentIds = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Where(st => candidateStudentIds.Contains(st.StudentId) && !candidateStudentTermIds.Contains(st.StudentTermId))
            .Select(st => st.StudentId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var blockedIdSet = blockedStudentIds.ToHashSet();
        var deletable = candidates.Where(st => !blockedIdSet.Contains(st.StudentId)).ToList();
        var skippedHasOtherTerms = candidates.Count(st => blockedIdSet.Contains(st.StudentId));

        if (deletable.Count == 0)
        {
            if (skippedPlaced == studentTerms.Count)
                return Result<BulkWithdrawStudentsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.AllStudentsPlaced));

            return Result<BulkWithdrawStudentsResponse>.Success(
                new BulkWithdrawStudentsResponse
                {
                    WithdrawnCount = 0,
                    DeletedFromSystemCount = 0,
                    SkippedPlacedCount = skippedPlaced,
                    SkippedAlreadyWithdrawnCount = skippedWithdrawn,
                    SkippedHasOtherTermsCount = skippedHasOtherTerms
                },
                _messageService.GetMessage(MessageKeys.StudentTerms.BulkWithdrawSuccess));
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var userIds = deletable.Select(st => st.Student.User.UserId).Distinct().ToList();

            var activeTokens = await _unitOfWork.Repository<RefreshToken>()
                .Query()
                .Where(rt => userIds.Contains(rt.UserId) && !rt.IsRevoked)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.IsRevoked = true;
                token.UpdatedAt = now;
                await _unitOfWork.Repository<RefreshToken>().UpdateAsync(token, cancellationToken);
            }

            foreach (var st in deletable)
            {
                st.EnrollmentStatus = EnrollmentStatus.Withdrawn;
                st.UpdatedBy = userId;
                st.UpdatedAt = now;

                var user = st.Student.User;
                var suffix = $"_deleted_{now:yyyyMMddHHmmssfff}";
                user.UpdateEmail($"{user.Email}{suffix}");
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    user.UpdateProfile(
                        user.FullName,
                        $"{user.PhoneNumber}{suffix}",
                        user.AvatarUrl,
                        user.Gender,
                        user.DateOfBirth,
                        user.Address);
                }

                st.DeletedBy = userId;
                await _unitOfWork.Repository<StudentTerm>().DeleteAsync(st, cancellationToken);
                await _unitOfWork.Repository<Student>().DeleteAsync(st.Student, cancellationToken);
                await _unitOfWork.Repository<User>().DeleteAsync(user, cancellationToken);
            }

            term.TotalEnrolled = Math.Max(0, term.TotalEnrolled - deletable.Count);
            term.TotalUnplaced = Math.Max(0, term.TotalUnplaced - deletable.Count);
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

        _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogBulkWithdrawn), deletable.Count, request.TermId, userId);

        return Result<BulkWithdrawStudentsResponse>.Success(
            new BulkWithdrawStudentsResponse
            {
                WithdrawnCount = deletable.Count,
                DeletedFromSystemCount = deletable.Count,
                SkippedPlacedCount = skippedPlaced,
                SkippedAlreadyWithdrawnCount = skippedWithdrawn,
                SkippedHasOtherTermsCount = skippedHasOtherTerms
            },
            _messageService.GetMessage(MessageKeys.StudentTerms.BulkWithdrawSuccess));
    }
}
