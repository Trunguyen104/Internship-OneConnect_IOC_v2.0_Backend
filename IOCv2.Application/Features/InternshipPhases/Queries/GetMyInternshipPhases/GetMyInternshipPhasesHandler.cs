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

        var isMentor = string.Equals(_currentUserService.Role, "Mentor", StringComparison.OrdinalIgnoreCase);

        return isMentor
            ? await HandleMentorAsync(userId, cancellationToken)
            : await HandleStudentAsync(userId, cancellationToken);
    }

    // ── Student branch ────────────────────────────────────────────────────────
    private async Task<Result<List<GetMyInternshipPhasesResponse>>> HandleStudentAsync(
        Guid userId, CancellationToken cancellationToken)
    {
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

        var groups = await _unitOfWork.Repository<InternshipGroup>()
            .Query()
            .Include(g => g.Enterprise)
            .Include(g => g.Mentor).ThenInclude(m => m!.User)
            .Include(g => g.InternshipPhase)
            .Include(g => g.Members)
            .Where(g => g.DeletedAt == null
                     && g.InternshipPhase != null
                     && g.InternshipPhase.DeletedAt == null
                     && g.Members.Any(m => m.StudentId == student.StudentId))
            .OrderByDescending(g => g.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var response = await BuildResponseAsync(groups, cancellationToken);

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogMyPhasesSuccess),
            student.StudentId, userId, response.Count);

        return Result<List<GetMyInternshipPhasesResponse>>.Success(response);
    }

    // ── Mentor branch ─────────────────────────────────────────────────────────
    private async Task<Result<List<GetMyInternshipPhasesResponse>>> HandleMentorAsync(
        Guid userId, CancellationToken cancellationToken)
    {
        var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>()
            .Query()
            .Where(eu => eu.UserId == userId)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (enterpriseUser == null)
        {
            _logger.LogWarning(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogMentorUserNotFound),
                userId);
            return Result<List<GetMyInternshipPhasesResponse>>.NotFound(
                _messageService.GetMessage(MessageKeys.InternshipPhase.MentorEnterpriseUserNotFound));
        }

        // Lấy tất cả nhóm mà Mentor đang phụ trách, kèm Phase tương ứng
        var groups = await _unitOfWork.Repository<InternshipGroup>()
            .Query()
            .Include(g => g.Enterprise)
            .Include(g => g.Mentor).ThenInclude(m => m!.User)
            .Include(g => g.InternshipPhase)
            .Include(g => g.Members)
            .Where(g => g.DeletedAt == null
                     && g.MentorId == enterpriseUser.EnterpriseUserId
                     && g.InternshipPhase != null
                     && g.InternshipPhase.DeletedAt == null)
            .OrderByDescending(g => g.CreatedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var response = await BuildResponseAsync(groups, cancellationToken);

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogMyMentorPhasesSuccess),
            enterpriseUser.EnterpriseUserId, userId, response.Count);

        return Result<List<GetMyInternshipPhasesResponse>>.Success(response);
    }

    // ── Shared projection ────────────────────────────────────────────────────
    private async Task<List<GetMyInternshipPhasesResponse>> BuildResponseAsync(
        List<InternshipGroup> groups, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var internshipIds = groups.Select(g => g.InternshipId).ToList();

        var projectLookup = internshipIds.Count == 0
            ? new Dictionary<Guid, Project>()
            : (await _unitOfWork.Repository<Project>()
                .Query()
                .Where(p => p.DeletedAt == null && p.InternshipId.HasValue && internshipIds.Contains(p.InternshipId.Value))
                .AsNoTracking()
                .ToListAsync(cancellationToken))
                .GroupBy(p => p.InternshipId!.Value)
                .ToDictionary(grp => grp.Key, grp => grp.First());

        // Deduplicate by PhaseId — take most recently created group per phase
        var phaseGroups = groups
            .GroupBy(g => g.InternshipPhase!.PhaseId)
            .Select(grp => grp.OrderByDescending(g => g.CreatedAt).First())
            .ToList();

        return phaseGroups.Select(group =>
        {
            var phase = group.InternshipPhase!;
            var project = projectLookup.GetValueOrDefault(group.InternshipId);
            var effectivePhaseStatus = ResolveEffectivePhaseStatus(phase, today);

            return new GetMyInternshipPhasesResponse
            {
                PhaseId        = phase.PhaseId,
                PhaseName      = phase.Name,
                PhaseStatus    = effectivePhaseStatus,
                InternshipGroupId = group.InternshipId,
                EnterpriseName = group.Enterprise?.Name,
                MentorName     = group.Mentor?.User?.FullName,
                ProjectName    = project?.ProjectName,
                JourneyStep    = CalculateJourneyStep(effectivePhaseStatus, group.Status),
                StartDate      = phase.StartDate,
                EndDate        = phase.EndDate
            };
        }).ToList();
    }

    private static InternshipPhaseStatus ResolveEffectivePhaseStatus(InternshipPhase phase, DateOnly today)
    {
        // Keep explicit manual states stable; infer operational state from dates for Open/InProgress.
        if (phase.Status == InternshipPhaseStatus.Draft || phase.Status == InternshipPhaseStatus.Closed)
        {
            return phase.Status;
        }

        return phase.GetLifecycleStatus(today) switch
        {
            InternshipPhaseLifecycleStatus.Upcoming => InternshipPhaseStatus.Open,
            InternshipPhaseLifecycleStatus.Active => InternshipPhaseStatus.InProgress,
            InternshipPhaseLifecycleStatus.Ended => InternshipPhaseStatus.Closed,
            _ => phase.Status
        };
    }

    /// <summary>
    /// Journey steps:
    /// 1 = Draft/Open, user chưa có group active
    /// 2 = Open, user có group active (đã phân nhóm)
    /// 3 = InProgress, đang thực tập
    /// 4 = Group archived (đã hoàn thành group) và phase đã InProgress/Closed
    /// 5 = Phase closed (kết thúc toàn bộ)
    /// </summary>
    private static int CalculateJourneyStep(InternshipPhaseStatus phaseStatus, GroupStatus groupStatus)
    {
        if (phaseStatus == InternshipPhaseStatus.Closed) return 5;
        if (groupStatus == GroupStatus.Archived) return 4;
        if (phaseStatus == InternshipPhaseStatus.InProgress && groupStatus == GroupStatus.Active) return 3;
        if (groupStatus == GroupStatus.Active) return 2;
        return 1;
    }
}
