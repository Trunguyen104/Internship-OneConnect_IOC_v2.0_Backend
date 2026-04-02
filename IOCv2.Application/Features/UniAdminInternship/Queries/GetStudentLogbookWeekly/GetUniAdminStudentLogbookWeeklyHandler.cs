using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.UniAdminInternship.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.UniAdminInternship.Queries.GetStudentLogbookWeekly;

public class GetUniAdminStudentLogbookWeeklyHandler
    : IRequestHandler<GetUniAdminStudentLogbookWeeklyQuery, Result<GetUniAdminStudentLogbookWeeklyResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetUniAdminStudentLogbookWeeklyHandler> _logger;

    public GetUniAdminStudentLogbookWeeklyHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetUniAdminStudentLogbookWeeklyHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetUniAdminStudentLogbookWeeklyResponse>> Handle(
        GetUniAdminStudentLogbookWeeklyQuery request,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            return Result<GetUniAdminStudentLogbookWeeklyResponse>.Failure(
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
            return Result<GetUniAdminStudentLogbookWeeklyResponse>.Failure(
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
                return Result<GetUniAdminStudentLogbookWeeklyResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.TermNotFound),
                    ResultErrorType.NotFound);
            }

            if (term.UniversityId != universityId)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.UniAdminInternship.LogTermAccessDenied),
                    currentUserId, term.TermId, universityId);
                return Result<GetUniAdminStudentLogbookWeeklyResponse>.Failure(
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
                return Result<GetUniAdminStudentLogbookWeeklyResponse>.Failure(
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
            return Result<GetUniAdminStudentLogbookWeeklyResponse>.Failure(
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

        var group = internStudent?.InternshipGroup;
        var response = new GetUniAdminStudentLogbookWeeklyResponse
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
            Weeks = new List<UniAdminWeeklyLogbookDto>()
        };

        if (group != null)
        {
            var studentLogbooks = await _unitOfWork.Repository<Logbook>().Query()
                .Include(l => l.WorkItems)
                .AsNoTracking()
                .Where(l =>
                    l.InternshipId == internStudent!.InternshipId
                    && l.StudentId == request.StudentId
                    && l.DeletedAt == null)
                .OrderBy(l => l.DateReport)
                .ToListAsync(cancellationToken);

            response.Weeks = UniAdminLogbookCalculator.BuildWeeklyLogbooks(
                studentLogbooks,
                internStudent!.JoinedAt,
                group.EndDate,
                _messageService);
        }

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.UniAdminInternship.LogGetLogbookWeekly),
            currentUserId, request.StudentId, term.TermId);

        return Result<GetUniAdminStudentLogbookWeeklyResponse>.Success(
            response,
            _messageService.GetMessage(MessageKeys.UniAdminInternship.StudentLogbookWeeklyRetrieved));
    }
}

