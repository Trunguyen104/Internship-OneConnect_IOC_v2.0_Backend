using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StudentTerms.Commands.BulkWithdrawStudents;

public class BulkWithdrawStudentsHandler
    : IRequestHandler<BulkWithdrawStudentsCommand, Result<BulkWithdrawStudentsResponse>>
{
    private readonly IBackgroundEmailSender _emailSender;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<BulkWithdrawStudentsHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;

    public BulkWithdrawStudentsHandler(IUnitOfWork unitOfWork, IMessageService messageService,
        ILogger<BulkWithdrawStudentsHandler> logger, ICurrentUserService currentUserService,
        IBackgroundEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
        _emailSender = emailSender;
    }

    public async Task<Result<BulkWithdrawStudentsResponse>> Handle(
        BulkWithdrawStudentsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.StudentTermIds == null || request.StudentTermIds.Count == 0)
                return Result<BulkWithdrawStudentsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.BulkWithdrawEmptyList));

            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            // Validate term
            var term = await _unitOfWork.Repository<Term>().Query()
                .FirstOrDefaultAsync(t => t.TermId == request.TermId, cancellationToken);

            if (term == null)
                return Result<BulkWithdrawStudentsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.TermNotFound), ResultErrorType.NotFound);

            if (!isSuperAdmin)
            {
                var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
                    .Where(uu => uu.UserId == userId).FirstOrDefaultAsync(cancellationToken);
                if (universityUser == null || universityUser.UniversityId != term.UniversityId)
                    return Result<BulkWithdrawStudentsResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.StudentTerms.AccessDenied), ResultErrorType.Forbidden);
            }

            // Load all requested student-term records (include Student/User for email notification)
            var studentTerms = await _unitOfWork.Repository<StudentTerm>().Query()
                .Include(st => st.Student).ThenInclude(s => s.User)
                .Where(st => request.StudentTermIds.Contains(st.StudentTermId) && st.TermId == request.TermId)
                .ToListAsync(cancellationToken);

            // Detect IDs that don't belong to this term (not found in the loaded list)
            var foundIds = studentTerms.Select(st => st.StudentTermId).ToHashSet();
            var notFoundIds = request.StudentTermIds.Where(id => !foundIds.Contains(id)).ToList();
            if (notFoundIds.Count > 0)
                return Result<BulkWithdrawStudentsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.NotFound), ResultErrorType.NotFound);

            var withdrawable = studentTerms
                .Where(st => st.PlacementStatus == PlacementStatus.Unplaced && st.EnrollmentStatus == EnrollmentStatus.Active)
                .ToList();
            var skippedPlaced = studentTerms.Count(st => st.PlacementStatus == PlacementStatus.Placed);
            var skippedAlreadyWithdrawn = studentTerms.Count(st => st.EnrollmentStatus == EnrollmentStatus.Withdrawn);

            if (withdrawable.Count == 0 && skippedPlaced == request.StudentTermIds.Count)
                return Result<BulkWithdrawStudentsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.StudentTerms.BulkWithdrawAllPlaced));

            // Withdraw them
            foreach (var st in withdrawable)
                st.EnrollmentStatus = EnrollmentStatus.Withdrawn;

            if (withdrawable.Count > 0)
            {
                term.TotalEnrolled = Math.Max(0, term.TotalEnrolled - withdrawable.Count);
                term.TotalUnplaced = Math.Max(0, term.TotalUnplaced - withdrawable.Count);
                await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);
                foreach (var st in withdrawable)
                    await _unitOfWork.Repository<StudentTerm>().UpdateAsync(st, cancellationToken);
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Fire-and-forget notifications to withdraw students (mirrors WithdrawStudentHandler pattern)
            foreach (var st in withdrawable)
            {
                var studentEmail = st.Student?.User?.Email;
                if (!string.IsNullOrEmpty(studentEmail))
                {
                    var subject = _messageService.GetMessage(MessageKeys.StudentTerms.NotifyWithdrawSubject);
                    var body = string.Format(_messageService.GetMessage(MessageKeys.StudentTerms.NotifyWithdrawBody), term.Name);
                    await _emailSender.EnqueueEmailAsync(studentEmail, subject, body,
                        auditTargetId: st.StudentTermId, performedByEmployeeId: userId, cancellationToken: cancellationToken);
                }
            }

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogWithdrawn),
                withdrawable.Count, userId);

            var message = string.Format(
                _messageService.GetMessage(MessageKeys.StudentTerms.BulkWithdrawSuccess),
                withdrawable.Count, skippedPlaced);

            return Result<BulkWithdrawStudentsResponse>.Success(
                new BulkWithdrawStudentsResponse
                {
                    WithdrawnCount = withdrawable.Count,
                    SkippedPlacedCount = skippedPlaced,
                    SkippedAlreadyWithdrawnCount = skippedAlreadyWithdrawn
                }, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.StudentTerms.LogError));
            throw;
        }
    }
}
