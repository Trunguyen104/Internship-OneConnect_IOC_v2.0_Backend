using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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
    private readonly IBackgroundEmailSender _emailSender;
    private readonly ILogger<BulkWithdrawStudentsHandler> _logger;

    public BulkWithdrawStudentsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        IBackgroundEmailSender emailSender,
        ILogger<BulkWithdrawStudentsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _emailSender = emailSender;
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

        // Load all studentTerms matching the IDs and TermId
        var studentTerms = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .Include(st => st.Student).ThenInclude(s => s.User)
            .Where(st => request.StudentTermIds.Contains(st.StudentTermId) && st.TermId == request.TermId)
            .ToListAsync(cancellationToken);

        // Check for IDs not found in this term
        var foundIds = studentTerms.Select(st => st.StudentTermId).ToHashSet();
        var notFoundIds = request.StudentTermIds.Where(id => !foundIds.Contains(id)).ToList();
        if (notFoundIds.Count > 0)
            return Result<BulkWithdrawStudentsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.NotFound), ResultErrorType.NotFound);

        // Classify
        var withdrawable = studentTerms
            .Where(st => st.EnrollmentStatus == EnrollmentStatus.Active && st.PlacementStatus == PlacementStatus.Unplaced)
            .ToList();
        var skippedPlaced = studentTerms.Count(st => st.PlacementStatus == PlacementStatus.Placed);
        var skippedWithdrawn = studentTerms.Count(st => st.EnrollmentStatus == EnrollmentStatus.Withdrawn);

        if (withdrawable.Count == 0 && skippedPlaced == studentTerms.Count)
            return Result<BulkWithdrawStudentsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.StudentTerms.AllStudentsPlaced));

        // Withdraw all withdrawable
        foreach (var st in withdrawable)
        {
            st.EnrollmentStatus = EnrollmentStatus.Withdrawn;
            st.UpdatedBy = userId;
            st.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.Repository<StudentTerm>().UpdateAsync(st, cancellationToken);
        }

        if (withdrawable.Count > 0)
        {
            term.TotalEnrolled -= withdrawable.Count;
            term.TotalUnplaced -= withdrawable.Count;
            await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);
        }

        await _unitOfWork.SaveChangeAsync(cancellationToken);

        // Fire-and-forget emails
        var emailSubject = _messageService.GetMessage(MessageKeys.StudentTerms.EmailSubjectWithdraw);
        var emailBody = _messageService.GetMessage(MessageKeys.StudentTerms.EmailBodyWithdraw, term.Name);
        foreach (var st in withdrawable)
        {
            _ = _emailSender.EnqueueEmailAsync(
                st.Student.User.Email,
                emailSubject,
                emailBody,
                st.StudentTermId,
                userId,
                CancellationToken.None);
        }

        _logger.LogInformation(_messageService.GetMessage(MessageKeys.StudentTerms.LogBulkWithdrawn), withdrawable.Count, request.TermId, userId);

        return Result<BulkWithdrawStudentsResponse>.Success(
            new BulkWithdrawStudentsResponse
            {
                WithdrawnCount = withdrawable.Count,
                SkippedPlacedCount = skippedPlaced,
                SkippedAlreadyWithdrawnCount = skippedWithdrawn
            },
            _messageService.GetMessage(MessageKeys.StudentTerms.BulkWithdrawSuccess));
    }
}
