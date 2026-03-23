using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Enterprises.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Enterprises.Queries.GetActiveTerms;

public class GetActiveTermsForEnterpriseHandler
    : IRequestHandler<GetActiveTermsForEnterpriseQuery, Result<GetActiveTermsForEnterpriseResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetActiveTermsForEnterpriseHandler> _logger;

    public GetActiveTermsForEnterpriseHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ICacheService cacheService,
        ILogger<GetActiveTermsForEnterpriseHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<GetActiveTermsForEnterpriseResponse>> Handle(
        GetActiveTermsForEnterpriseQuery request, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(_currentUserService.UserId!);
        var role = _currentUserService.Role;
        var isMentor = string.Equals(role, "Mentor", StringComparison.OrdinalIgnoreCase);

        // Lookup EnterpriseUser
        var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>()
            .Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(eu => eu.UserId == userId, cancellationToken);

        if (enterpriseUser == null)
            return Result<GetActiveTermsForEnterpriseResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Enterprise.HRNotAssociatedWithEnterprise),
                ResultErrorType.Forbidden);

        var enterpriseId = enterpriseUser.EnterpriseId;

        // Try cache
        var cacheKey = EnterpriseCacheKeys.ActiveTerms(enterpriseId, enterpriseUser.EnterpriseUserId, isMentor, request.UniversityId);
        var cached = await _cacheService.GetAsync<GetActiveTermsForEnterpriseResponse>(cacheKey, cancellationToken);
        if (cached != null)
            return Result<GetActiveTermsForEnterpriseResponse>.Success(cached);

        // Resolve term IDs in scope for this user — only Active groups count
        IQueryable<InternshipGroup> groupQuery;
        if (isMentor)
        {
            // Mentor: only active groups where MentorId = this user's EnterpriseUserId
            groupQuery = _unitOfWork.Repository<InternshipGroup>()
                .Query()
                .AsNoTracking()
                .Where(ig => ig.MentorId == enterpriseUser.EnterpriseUserId
                             && ig.Status == GroupStatus.Active);
        }
        else
        {
            // HR / EnterpriseAdmin: all active groups belonging to the enterprise
            groupQuery = _unitOfWork.Repository<InternshipGroup>()
                .Query()
                .AsNoTracking()
                .Where(ig => ig.EnterpriseId == enterpriseId
                             && ig.Status == GroupStatus.Active);
        }

        var termIdsInScope = await groupQuery
            .Select(ig => ig.TermId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (termIdsInScope.Count == 0)
        {
            var empty = new GetActiveTermsForEnterpriseResponse { Terms = new List<ActiveTermTimelineResponse>() };
            await _cacheService.SetAsync(cacheKey, empty, EnterpriseCacheKeys.Expiration.ActiveTerms, cancellationToken);
            return Result<GetActiveTermsForEnterpriseResponse>.Success(empty);
        }

        // Load matching Terms (Active only) — include University
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var termsQuery = _unitOfWork.Repository<Term>()
            .Query()
            .AsNoTracking()
            .Include(t => t.University)
            .Where(t =>
                termIdsInScope.Contains(t.TermId) &&
                t.Status == TermStatus.Open &&
                t.StartDate <= today &&
                t.EndDate >= today);

        if (request.UniversityId.HasValue)
            termsQuery = termsQuery.Where(t => t.UniversityId == request.UniversityId.Value);

        var terms = await termsQuery.ToListAsync(cancellationToken);

        if (terms.Count == 0)
        {
            var empty = new GetActiveTermsForEnterpriseResponse { Terms = new List<ActiveTermTimelineResponse>() };
            await _cacheService.SetAsync(cacheKey, empty, EnterpriseCacheKeys.Expiration.ActiveTerms, cancellationToken);
            return Result<GetActiveTermsForEnterpriseResponse>.Success(empty);
        }

        // Load EvaluationCycles for all active terms in one batch
        // Exclude Cancelled cycles — they have no meaningful deadline to display
        var activeTermIds = terms.Select(t => t.TermId).ToList();
        var cycles = await _unitOfWork.Repository<EvaluationCycle>()
            .Query()
            .AsNoTracking()
            .Where(ec => activeTermIds.Contains(ec.TermId)
                         && ec.Status != EvaluationCycleStatus.Cancelled)
            .ToListAsync(cancellationToken);

        var cyclesByTermId = cycles.GroupBy(ec => ec.TermId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var termResponses = terms.Select(term =>
        {
            var totalDays = term.EndDate.DayNumber - term.StartDate.DayNumber;
            var daysElapsed = today.DayNumber - term.StartDate.DayNumber;
            var daysRemaining = term.EndDate.DayNumber - today.DayNumber;
            var progressPercent = totalDays > 0
                ? Math.Round((double)daysElapsed / totalDays * 100, 1)
                : 0;

            var deadlines = new List<DeadlineInfo>();
            if (cyclesByTermId.TryGetValue(term.TermId, out var termCycles))
            {
                deadlines = termCycles
                    .OrderBy(ec => ec.EndDate)
                    .Select(ec =>
                    {
                        // Compare by calendar day (UTC) to avoid partial-day Ceiling bugs
                        var deadlineDay = DateOnly.FromDateTime(ec.EndDate.ToUniversalTime());
                        var daysUntil = deadlineDay.DayNumber - today.DayNumber;
                        return new DeadlineInfo
                        {
                            CycleId = ec.CycleId,
                            CycleName = ec.Name,
                            DeadlineType = "EvaluationSubmission",
                            DeadlineDate = ec.EndDate,
                            DaysUntilDeadline = daysUntil,
                            IsWarning = daysUntil >= 0 && daysUntil <= 7,
                            IsOverdue = daysUntil < 0,
                            CycleStatus = ec.Status
                        };
                    })
                    .ToList();
            }

            return new ActiveTermTimelineResponse
            {
                TermId = term.TermId,
                TermName = term.Name,
                UniversityId = term.UniversityId,
                UniversityName = term.University.Name,
                StartDate = term.StartDate,
                EndDate = term.EndDate,
                Status = TermStatusHelper.GetComputedStatus(term.StartDate, term.EndDate, term.Status),
                TotalDays = totalDays,
                DaysElapsed = daysElapsed,
                DaysRemaining = daysRemaining,
                ProgressPercent = progressPercent,
                Deadlines = deadlines
            };
        }).ToList();

        var response = new GetActiveTermsForEnterpriseResponse { Terms = termResponses };
        await _cacheService.SetAsync(cacheKey, response, EnterpriseCacheKeys.Expiration.ActiveTerms, cancellationToken);

        return Result<GetActiveTermsForEnterpriseResponse>.Success(response);
    }
}
