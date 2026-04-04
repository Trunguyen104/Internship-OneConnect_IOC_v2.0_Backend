using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Enterprises.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Enterprises.Queries.GetActivePhases;

public class GetActivePhasesForEnterpriseHandler
    : IRequestHandler<GetActivePhasesForEnterpriseQuery, Result<GetActivePhasesForEnterpriseResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetActivePhasesForEnterpriseHandler> _logger;

    public GetActivePhasesForEnterpriseHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        IMessageService messageService,
        ILogger<GetActivePhasesForEnterpriseHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetActivePhasesForEnterpriseResponse>> Handle(
        GetActivePhasesForEnterpriseQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<GetActivePhasesForEnterpriseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.ActivePhases.InvalidUserId), ResultErrorType.Unauthorized);

            var isMentor = string.Equals(_currentUserService.Role, UserRole.Mentor.ToString(), StringComparison.OrdinalIgnoreCase);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<GetActivePhasesForEnterpriseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.ActivePhases.EnterpriseUserNotFound), ResultErrorType.Forbidden);

            var enterpriseId = enterpriseUser.EnterpriseId;

            // Try cache
            var cacheKey = EnterpriseCacheKeys.ActivePhases(enterpriseId, enterpriseUser.EnterpriseUserId, isMentor);

            var cached = await _cacheService.GetAsync<GetActivePhasesForEnterpriseResponse>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<GetActivePhasesForEnterpriseResponse>.Success(cached);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Fetch active phrases where the enterprise is involved
            var phasesQuery = _unitOfWork.Repository<InternshipPhase>().Query().AsNoTracking()
                .Where(p => p.EnterpriseId == enterpriseId &&
                            p.Status == InternshipPhaseStatus.Open &&
                            p.StartDate <= today &&
                            p.EndDate >= today);

            // If it's a mentor, only return phases where they are assigned to an internship group.
            if (isMentor)
            {
                phasesQuery = phasesQuery.Where(p => p.InternshipGroups.Any(ig => ig.MentorId == enterpriseUser.EnterpriseUserId));
            }

            var phases = await phasesQuery
                .OrderBy(p => p.EndDate)
                .ToListAsync(cancellationToken);

            if (phases.Count == 0)
            {
                var noPhasesKey = isMentor
                    ? MessageKeys.ActivePhases.NoActivePhasesFoundForMentor
                    : MessageKeys.ActivePhases.NoActivePhasesFoundForEnterprise;
                    
                return Result<GetActivePhasesForEnterpriseResponse>.Failure(
                    _messageService.GetMessage(noPhasesKey), ResultErrorType.NotFound);
            }

            var phaseIds = phases.Select(p => p.PhaseId).ToList();

            // Fetch Evaluation Cycles for these phases
            var cycles = await _unitOfWork.Repository<EvaluationCycle>().Query().AsNoTracking()
                .Where(ec => phaseIds.Contains(ec.PhaseId) && ec.Status != EvaluationCycleStatus.Cancelled)
                .OrderBy(ec => ec.EndDate)
                .ToListAsync(cancellationToken);

            var cyclesByPhase = cycles.GroupBy(c => c.PhaseId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var nowUtc = DateTime.UtcNow;

            var phaseResponses = phases.Select(phase =>
            {
                var totalDays = (phase.EndDate.DayNumber - phase.StartDate.DayNumber);
                var daysElapsed = Math.Max(0, today.DayNumber - phase.StartDate.DayNumber);
                var daysRemaining = Math.Max(0, phase.EndDate.DayNumber - today.DayNumber);
                var progressPercent = totalDays > 0 ? Math.Round((double)daysElapsed / totalDays * 100, 1) : 0;

                var deadlines = cyclesByPhase.TryGetValue(phase.PhaseId, out var phaseCycles)
                    ? phaseCycles.Select(ec =>
                    {
                        var daysUntil = (int)Math.Ceiling((ec.EndDate - nowUtc).TotalDays);
                        return new PhaseDeadlineInfo
                        {
                            CycleId = ec.CycleId,
                            CycleName = ec.Name,
                            DeadlineDate = ec.EndDate,
                            DaysUntilDeadline = daysUntil,
                            IsOverdue = daysUntil < 0,
                            IsWarning = daysUntil >= 0 && daysUntil <= 7,
                            CycleStatus = ec.Status
                        };
                    }).ToList()
                    : new List<PhaseDeadlineInfo>();

                return new ActivePhaseTimelineResponse
                {
                    PhaseId = phase.PhaseId,
                    PhaseName = phase.Name,
                    StartDate = phase.StartDate,
                    EndDate = phase.EndDate,
                    Status = phase.Status,
                    TotalDays = totalDays,
                    DaysElapsed = daysElapsed,
                    DaysRemaining = daysRemaining,
                    ProgressPercent = progressPercent,
                    Deadlines = deadlines
                };
            }).ToList();

            var response = new GetActivePhasesForEnterpriseResponse { Phases = phaseResponses };

            await _cacheService.SetAsync(cacheKey, response, EnterpriseCacheKeys.Expiration.ActivePhases, cancellationToken);
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.ActivePhases.LogRetrieved), phaseResponses.Count, enterpriseId, isMentor);

            return Result<GetActivePhasesForEnterpriseResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ActivePhases.LogError), _currentUserService.UserId);
            return Result<GetActivePhasesForEnterpriseResponse>.Failure(
                _messageService.GetMessage(MessageKeys.ActivePhases.SystemError), ResultErrorType.InternalServerError);
        }
    }
}
