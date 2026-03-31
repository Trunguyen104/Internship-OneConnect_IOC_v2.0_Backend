using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentViolations;

public class GetUniAdminStudentViolationsHandler
    : IRequestHandler<GetUniAdminStudentViolationsQuery, Result<GetUniAdminStudentViolationsResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetUniAdminStudentViolationsHandler> _logger;

    public GetUniAdminStudentViolationsHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetUniAdminStudentViolationsHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetUniAdminStudentViolationsResponse>> Handle(
        GetUniAdminStudentViolationsQuery request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<GetUniAdminStudentViolationsResponse>.Failure(
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
            return Result<GetUniAdminStudentViolationsResponse>.Failure(
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
                return Result<GetUniAdminStudentViolationsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.TermNotFound),
                    ResultErrorType.NotFound);
            }

            if (term.UniversityId != universityId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermAccessDenied),
                    currentUserId, term.TermId, universityId);
                return Result<GetUniAdminStudentViolationsResponse>.Failure(
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
                return Result<GetUniAdminStudentViolationsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.NoOpenTermFound),
                    ResultErrorType.NotFound);
        }

        // Verify student belongs to this term
        var studentTerm = await _unitOfWork.Repository<StudentTerm>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(st =>
                st.TermId == term.TermId
                && st.StudentId == request.StudentId
                && st.EnrollmentStatus == EnrollmentStatus.Active
                && st.DeletedAt == null,
                cancellationToken);

        if (studentTerm == null)
        {
            var studentExists = await _unitOfWork.Repository<Student>().Query()
                .AsNoTracking()
                .AnyAsync(s => s.StudentId == request.StudentId && s.DeletedAt == null, cancellationToken);

            if (studentExists)
            {
                var belongsToAnotherUniversity = await _unitOfWork.Repository<StudentTerm>().Query()
                    .AsNoTracking()
                    .AnyAsync(st =>
                        st.StudentId == request.StudentId
                        && st.EnrollmentStatus == EnrollmentStatus.Active
                        && st.DeletedAt == null
                        && st.Term.UniversityId != universityId,
                        cancellationToken);

                if (belongsToAnotherUniversity)
                {
                    _logger.LogWarning(
                        _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermAccessDenied),
                        currentUserId, term.TermId, universityId);
                    return Result<GetUniAdminStudentViolationsResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentNotInUniversity),
                        ResultErrorType.Forbidden);
                }
            }

            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogStudentNotFound),
                request.StudentId, term.TermId, universityId);
            return Result<GetUniAdminStudentViolationsResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentNotFound),
                ResultErrorType.NotFound);
        }

        // Find student's InternshipGroup (scoped to enterprise)
        InternshipStudent? internStudent = null;
        if (studentTerm.EnterpriseId.HasValue)
        {
            internStudent = await _unitOfWork.Repository<InternshipStudent>().Query()
                .AsNoTracking()
                .Where(isv =>
                    isv.StudentId == request.StudentId
                    && isv.InternshipGroup.EnterpriseId == studentTerm.EnterpriseId
                    && isv.DeletedAt == null)
                .OrderByDescending(isv => isv.JoinedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        // Load violations scoped strictly to student's resolved internship group.
        var violations = internStudent == null
            ? new List<ViolationReport>()
            : await _unitOfWork.Repository<ViolationReport>().Query()
                .Include(v => v.InternshipGroup)
                .AsNoTracking()
                .Where(v =>
                    v.StudentId == request.StudentId
                    && v.InternshipGroupId == internStudent.InternshipId
                    && v.DeletedAt == null)
                .OrderByDescending(v => v.OccurredDate)
                .ToListAsync(cancellationToken);

        var items = violations.Select(v => new ViolationItemDto
        {
            ViolationReportId = v.ViolationReportId,
            OccurredDate = v.OccurredDate,
            ReportedAt = v.CreatedAt,
            Description = v.Description,
            InternshipGroupName = v.InternshipGroup.GroupName
        }).ToList();

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.UniAdminInternship.LogGetViolations),
            currentUserId, request.StudentId, term.TermId);

        return Result<GetUniAdminStudentViolationsResponse>.Success(
            new GetUniAdminStudentViolationsResponse { Violations = items },
            _messageService.GetMessage(MessageKeys.UniAdminInternship.ViolationsRetrieved));
    }
}
