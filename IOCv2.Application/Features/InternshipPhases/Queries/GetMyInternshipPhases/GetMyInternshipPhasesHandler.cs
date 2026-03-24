using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetMyInternshipPhases;

public class GetMyInternshipPhasesHandler
    : IRequestHandler<GetMyInternshipPhasesQuery, Result<List<GetMyInternshipPhasesResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetMyInternshipPhasesHandler> _logger;

    public GetMyInternshipPhasesHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ILogger<GetMyInternshipPhasesHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<List<GetMyInternshipPhasesResponse>>> Handle(
        GetMyInternshipPhasesQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentUserService.UserId)
            || !Guid.TryParse(_currentUserService.UserId, out var userId))
        {
            return Result<List<GetMyInternshipPhasesResponse>>.Failure(
                _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                ResultErrorType.Unauthorized);
        }

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogGettingMyPhases),
            userId);

        var student = await _unitOfWork.Repository<Student>()
            .Query()
            .Where(s => s.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (student == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogStudentNotFound),
                userId);
            return Result<List<GetMyInternshipPhasesResponse>>.NotFound(
                _messageService.GetMessage(MessageKeys.InternshipPhase.StudentNotFound));
        }

        // Lấy tất cả InternshipGroup mà sinh viên là thành viên, kèm Phase tương ứng
        var groups = await _unitOfWork.Repository<InternshipGroup>()
            .Query()
            .Include(g => g.Enterprise)
            .Include(g => g.Mentor)
                .ThenInclude(m => m!.User)
            .Include(g => g.InternshipPhase)
            .Include(g => g.Members)
            .Where(g => g.DeletedAt == null
                     && g.InternshipPhase != null
                     && g.InternshipPhase.DeletedAt == null  // BUG-06 FIX: exclude soft-deleted phases
                     && g.Members.Any(m => m.StudentId == student.StudentId))
            .OrderByDescending(g => g.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var internshipIds = groups.Select(g => g.InternshipId).ToList();

        var projectLookup = internshipIds.Count == 0
            ? new Dictionary<Guid, Project>()
            : (await _unitOfWork.Repository<Project>()
                .Query()
                .Where(p => p.DeletedAt == null && internshipIds.Contains(p.InternshipId))
                .AsNoTracking()
                .ToListAsync(cancellationToken))
                .GroupBy(p => p.InternshipId)
                .ToDictionary(grp => grp.Key, grp => grp.First());

        // BUG-J FIX: Deduplicate by PhaseId before projecting.
        // A student moved between groups (or added to multiple groups) in the same phase
        // would previously appear duplicated in the response. Group by PhaseId and take
        // the most recently created group per phase as the canonical record.
        var phaseGroups = groups
            .GroupBy(g => g.InternshipPhase!.PhaseId)
            .Select(grp => grp.OrderByDescending(g => g.CreatedAt).First())
            .ToList();

        var response = phaseGroups.Select(group =>
        {
            var phase = group.InternshipPhase!;
            var project = projectLookup.GetValueOrDefault(group.InternshipId);

            return new GetMyInternshipPhasesResponse
            {
                PhaseId = phase.PhaseId,
                PhaseName = phase.Name,
                PhaseStatus = phase.Status,
                InternshipGroupId = group.InternshipId,
                EnterpriseName = group.Enterprise?.Name,
                MentorName = group.Mentor?.User?.FullName,
                ProjectName = project?.ProjectName,
                JourneyStep = CalculateJourneyStep(phase.Status, group.Status),
                StartDate = phase.StartDate,
                EndDate = phase.EndDate
            };
        }).ToList();

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogMyPhasesSuccess),
            student.StudentId, userId, response.Count);

        return Result<List<GetMyInternshipPhasesResponse>>.Success(response);
    }

    /// <summary>
    /// Journey steps:
    /// 1 = Draft/Open, sinh viên chưa có group active
    /// 2 = Open, sinh viên có group active (đã phân nhóm)
    /// 3 = InProgress, đang thực tập
    /// 4 = Group archived (đã hoàn thành group) và phase đã InProgress/Closed
    /// 5 = Phase closed (kết thúc toàn bộ)
    /// </summary>
    private static int CalculateJourneyStep(InternshipPhaseStatus phaseStatus, GroupStatus groupStatus)
    {
        if (phaseStatus == InternshipPhaseStatus.Closed) return 5;
        // BUG-08 FIX: Archived group = step 4 regardless of phase status (previously Open+Archived fell to step 1)
        if (groupStatus == GroupStatus.Archived) return 4;
        if (phaseStatus == InternshipPhaseStatus.InProgress && groupStatus == GroupStatus.Active) return 3;
        if (groupStatus == GroupStatus.Active) return 2;
        return 1;
    }
}
