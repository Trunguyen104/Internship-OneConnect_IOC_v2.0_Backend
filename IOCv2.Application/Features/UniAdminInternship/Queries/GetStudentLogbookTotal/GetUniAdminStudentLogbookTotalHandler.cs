using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.UniAdminInternship.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbookTotal;

public class GetUniAdminStudentLogbookTotalHandler
    : IRequestHandler<GetUniAdminStudentLogbookTotalQuery, Result<GetUniAdminStudentLogbookTotalResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetUniAdminStudentLogbookTotalHandler> _logger;

    public GetUniAdminStudentLogbookTotalHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetUniAdminStudentLogbookTotalHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetUniAdminStudentLogbookTotalResponse>> Handle(
        GetUniAdminStudentLogbookTotalQuery request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<GetUniAdminStudentLogbookTotalResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                ResultErrorType.Unauthorized);

        var universityUser = await _unitOfWork.Repository<UniversityUser>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(uu => uu.UserId == currentUserId, cancellationToken);

        if (universityUser == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogUniversityUserNotFound),
                currentUserId);
            return Result<GetUniAdminStudentLogbookTotalResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.UniversityUserNotFound),
                ResultErrorType.Forbidden);
        }

        var universityId = universityUser.UniversityId;

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
                return Result<GetUniAdminStudentLogbookTotalResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.TermNotFound),
                    ResultErrorType.NotFound);
            }

            if (term.UniversityId != universityId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermAccessDenied),
                    currentUserId, term.TermId, universityId);
                return Result<GetUniAdminStudentLogbookTotalResponse>.Failure(
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
                return Result<GetUniAdminStudentLogbookTotalResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.NoOpenTermFound),
                    ResultErrorType.NotFound);
        }

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
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.LogStudentNotFound),
                request.StudentId, term.TermId, universityId);
            return Result<GetUniAdminStudentLogbookTotalResponse>.Failure(
                _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentNotFound),
                ResultErrorType.NotFound);
        }

        InternshipStudent? internStudent = null;
        if (studentTerm.EnterpriseId.HasValue)
        {
            internStudent = await _unitOfWork.Repository<InternshipStudent>().Query()
                .Include(isv => isv.InternshipGroup)
                    .ThenInclude(ig => ig.Enterprise)
                .Include(isv => isv.InternshipGroup)
                    .ThenInclude(ig => ig.Mentor)
                        .ThenInclude(m => m!.User)
                .AsNoTracking()
                .Where(isv =>
                    isv.StudentId == request.StudentId
                    && isv.InternshipGroup.EnterpriseId == studentTerm.EnterpriseId
                    && isv.DeletedAt == null)
                .OrderByDescending(isv => isv.JoinedAt)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var submittedCount = 0;
        var lateCount = 0;
        if (internStudent != null)
        {
            submittedCount = await _unitOfWork.Repository<Logbook>().Query()
                .AsNoTracking()
                .CountAsync(l =>
                    l.InternshipId == internStudent.InternshipId
                    && l.StudentId == request.StudentId
                    && l.DeletedAt == null,
                    cancellationToken);

            lateCount = await _unitOfWork.Repository<Logbook>().Query()
                .AsNoTracking()
                .CountAsync(l =>
                    l.InternshipId == internStudent.InternshipId
                    && l.StudentId == request.StudentId
                    && l.Status == LogbookStatus.LATE
                    && l.DeletedAt == null,
                    cancellationToken);
        }

        var group = internStudent?.InternshipGroup;
        var response = new GetUniAdminStudentLogbookTotalResponse
        {
            StudentId = request.StudentId,
            ResolvedTermId = term.TermId,
            InternshipGroupId = group?.InternshipId,
            InternshipGroupName = group?.GroupName,
            EnterpriseName = group?.Enterprise?.Name,
            MentorName = group?.Mentor?.User?.FullName,
            GroupStartDate = group?.StartDate,
            GroupEndDate = group?.EndDate,
            InternshipRole = internStudent?.Role.ToString(),
            JoinedAt = internStudent?.JoinedAt,
            Logbook = UniAdminLogbookCalculator.CalculateLogbookSummary(
                internStudent,
                group,
                submittedCount,
                lateCount)
        };

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.UniAdminInternship.LogGetLogbookTotal),
            currentUserId, request.StudentId, term.TermId);

        return Result<GetUniAdminStudentLogbookTotalResponse>.Success(
            response,
            _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentLogbookTotalRetrieved));
    }
}

