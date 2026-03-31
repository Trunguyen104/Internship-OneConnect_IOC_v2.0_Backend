using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.UniAdminInternship.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentDetail;

public class GetUniAdminStudentDetailHandler
    : IRequestHandler<GetUniAdminStudentDetailQuery, Result<GetUniAdminStudentDetailResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetUniAdminStudentDetailHandler> _logger;

    public GetUniAdminStudentDetailHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetUniAdminStudentDetailHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetUniAdminStudentDetailResponse>> Handle(
        GetUniAdminStudentDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<GetUniAdminStudentDetailResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                ResultErrorType.Unauthorized);

        // Get UniversityId
        var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(uu => uu.UserId == currentUserId, cancellationToken);

        if (universityUser == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogUniversityUserNotFound),
                currentUserId);
            return Result<GetUniAdminStudentDetailResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.UniversityUserNotFound),
                ResultErrorType.Forbidden);
        }

        var universityId = universityUser.UniversityId;

        // Resolve Term
        Term? term;
        if (request.TermId.HasValue)
        {
            term = await _unitOfWork.Repository<Term>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.TermId == request.TermId.Value, cancellationToken);

            if (term == null)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermNotFound),
                    request.TermId.Value);
                return Result<GetUniAdminStudentDetailResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.TermNotFound),
                    ResultErrorType.NotFound);
            }

            if (term.UniversityId != universityId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermAccessDenied),
                    currentUserId, term.TermId, universityId);
                return Result<GetUniAdminStudentDetailResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.TermAccessDenied),
                    ResultErrorType.Forbidden);
            }
        }
        else
        {
            term = await _unitOfWork.Repository<Term>().Query()
                .AsNoTracking()
                .Where(t => t.UniversityId == universityId && t.Status == TermStatus.Open)
                .OrderByDescending(t => t.StartDate)
                .FirstOrDefaultAsync(cancellationToken);

            if (term == null)
                return Result<GetUniAdminStudentDetailResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.NoOpenTermFound),
                    ResultErrorType.NotFound);
        }

        // Load StudentTerm (verify student belongs to this term+university)
        var studentTerm = await _unitOfWork.Repository<StudentTerm>().Query()
            .Include(st => st.Student)
                .ThenInclude(s => s.User)
            .Include(st => st.Enterprise)
            .AsNoTracking()
            .FirstOrDefaultAsync(st =>
                st.TermId == term.TermId
                && st.StudentId == request.StudentId
                && st.EnrollmentStatus == EnrollmentStatus.Active
                && st.DeletedAt == null,
                cancellationToken);

        if (studentTerm == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogStudentNotFound),
                request.StudentId, term.TermId, universityId);
            return Result<GetUniAdminStudentDetailResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentNotFound),
                ResultErrorType.NotFound);
        }

        var student = studentTerm.Student;
        var user = student.User;

        // Load InternshipStudent (if student has been placed at an enterprise)
        InternshipStudent? internStudent = null;
        if (studentTerm.EnterpriseId.HasValue)
        {
            internStudent = await _unitOfWork.Repository<InternshipStudent>().Query()
                .Include(isv => isv.InternshipGroup)
                    .ThenInclude(ig => ig.Mentor!)
                        .ThenInclude(m => m.User)
                .AsNoTracking()
                .Where(isv =>
                    isv.StudentId == request.StudentId
                    && isv.InternshipGroup.EnterpriseId == studentTerm.EnterpriseId
                    && isv.DeletedAt == null)
                .OrderByDescending(isv => isv.JoinedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var group = internStudent?.InternshipGroup;
        var hasGroup = group != null;

        // Check for pending application
        var hasPendingApp = await _unitOfWork.Repository<InternshipApplication>().Query()
            .AsNoTracking()
            .AnyAsync(app =>
                app.StudentId == request.StudentId
                && app.TermId == term.TermId
                && app.DeletedAt == null
                && (app.Status == InternshipApplicationStatus.Applied
                    || app.Status == InternshipApplicationStatus.Interviewing
                    || app.Status == InternshipApplicationStatus.Offered
                    || app.Status == InternshipApplicationStatus.PendingAssignment),
                cancellationToken);

        var uiStatus = DeriveUiStatus(studentTerm.PlacementStatus, hasGroup, hasPendingApp, group?.EndDate);

        // Violation count
        var violationCount = group != null
            ? await _unitOfWork.Repository<ViolationReport>().Query()
                .AsNoTracking()
                .CountAsync(v =>
                    v.StudentId == request.StudentId
                    && v.InternshipGroupId == group.InternshipId
                    && v.DeletedAt == null,
                    cancellationToken)
            : 0;

        // Published evaluation count
        var publishedEvalCount = 0;
        if (group != null)
        {
            publishedEvalCount = await _unitOfWork.Repository<Evaluation>().Query()
                .AsNoTracking()
                .CountAsync(e =>
                    e.InternshipId == group.InternshipId
                    && e.StudentId == request.StudentId
                    && e.Status == EvaluationStatus.Published
                    && e.DeletedAt == null,
                    cancellationToken);
        }

        var response = new GetUniAdminStudentDetailResponse
        {
            StudentId = student.StudentId,
            StudentCode = user.UserCode,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            ClassName = student.ClassName,
            Major = student.Major,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            DateOfBirth = user.DateOfBirth,
            ResolvedTermId = term.TermId,
            TermName = term.Name,
            TermStartDate = term.StartDate,
            TermEndDate = term.EndDate,
            InternshipStatus = uiStatus,
            EnterpriseId = studentTerm.EnterpriseId,
            EnterpriseName = studentTerm.Enterprise?.Name,
            EnterprisePosition = group?.Description,
            InternshipPhaseStartDate = group?.StartDate,
            InternshipPhaseEndDate = group?.EndDate,
            MentorId = group?.MentorId,
            MentorName = group?.Mentor?.User?.FullName,
            MentorEmail = group?.Mentor?.User?.Email,
            ViolationCount = violationCount,
            PublishedEvaluationCount = publishedEvalCount
        };

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.UniAdminInternship.LogGetStudentDetail),
            currentUserId, request.StudentId, term.TermId);

        return Result<GetUniAdminStudentDetailResponse>.Success(
            response,
            _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentDetailRetrieved));
    }

    private static InternshipUiStatus DeriveUiStatus(
        PlacementStatus placementStatus,
        bool hasGroup,
        bool hasPendingApp,
        DateTime? groupEndDate)
    {
        if (placementStatus == PlacementStatus.Unplaced)
            return hasPendingApp ? InternshipUiStatus.PendingConfirmation : InternshipUiStatus.Unplaced;

        if (hasGroup && groupEndDate.HasValue && groupEndDate.Value.Date < DateTime.UtcNow.Date)
            return InternshipUiStatus.Completed;

        return hasGroup ? InternshipUiStatus.Active : InternshipUiStatus.NoGroup;
    }

}
