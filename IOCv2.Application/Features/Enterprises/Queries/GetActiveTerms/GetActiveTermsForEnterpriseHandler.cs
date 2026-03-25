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
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetActiveTermsForEnterpriseHandler> _logger;

    public GetActiveTermsForEnterpriseHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        IMessageService messageService,
        ILogger<GetActiveTermsForEnterpriseHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<GetActiveTermsForEnterpriseResponse>> Handle(
        GetActiveTermsForEnterpriseQuery request, CancellationToken cancellationToken)
    {
        try
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<GetActiveTermsForEnterpriseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.ActiveTerms.InvalidUserId), ResultErrorType.Unauthorized);

            var isMentor = string.Equals(_currentUserService.Role, UserRole.Mentor.ToString(),
                StringComparison.OrdinalIgnoreCase);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query().AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<GetActiveTermsForEnterpriseResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.ActiveTerms.EnterpriseUserNotFound), ResultErrorType.Forbidden);

            var enterpriseId = enterpriseUser.EnterpriseId;

            // Try cache
            var cacheKey = EnterpriseCacheKeys.ActiveTerms(
                enterpriseId, enterpriseUser.EnterpriseUserId, isMentor, request.UniversityId);

            var cached = await _cacheService.GetAsync<GetActiveTermsForEnterpriseResponse>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<GetActiveTermsForEnterpriseResponse>.Success(cached);

            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            // Query active terms linked to this enterprise via InternshipGroups
            var termsQuery = _unitOfWork.Repository<Term>().Query().AsNoTracking()
                .Include(t => t.University)
                .Where(t =>
                    t.Status == TermStatus.Open &&
                    t.StartDate <= today &&
                    t.EndDate >= today);

            // Filter: terms where enterprise has approved applications
            termsQuery = termsQuery.Where(t =>
                _unitOfWork.Repository<InternshipApplication>().Query()
                    .Any(a => a.TermId == t.TermId && a.EnterpriseId == enterpriseId));

            if (request.UniversityId.HasValue)
                termsQuery = termsQuery.Where(t => t.UniversityId == request.UniversityId.Value);

            var terms = await termsQuery
                .OrderBy(t => t.EndDate)
                .ToListAsync(cancellationToken);

            if (terms.Count == 0)
            {
                var noTermsKey = isMentor
                    ? MessageKeys.ActiveTerms.NoActiveTermsFoundForMentor
                    : MessageKeys.ActiveTerms.NoActiveTermsFoundForEnterprise;
                return Result<GetActiveTermsForEnterpriseResponse>.Failure(
                    _messageService.GetMessage(noTermsKey), ResultErrorType.NotFound);
            }

            // Lấy EvaluationCycles cho enterprise (bỏ qua Cancelled)
            var cycles = await _unitOfWork.Repository<EvaluationCycle>().Query().AsNoTracking()
                .Where(ec => ec.InternshipPhase.EnterpriseId == enterpriseId &&
                             ec.Status != EvaluationCycleStatus.Cancelled)
                .OrderBy(ec => ec.EndDate)
                .ToListAsync(cancellationToken);

            // Cycles are now phase-based, not term-based; provide empty lookup for term mapping
            var cyclesByTerm = new Dictionary<Guid, List<EvaluationCycle>>();

            var nowUtc = DateTime.UtcNow;

            var termResponses = terms.Select(term =>
            {
                // Timeline
                var totalDays = (term.EndDate.DayNumber - term.StartDate.DayNumber);
                var daysElapsed = Math.Max(0, today.DayNumber - term.StartDate.DayNumber);
                var daysRemaining = Math.Max(0, term.EndDate.DayNumber - today.DayNumber);
                var progressPercent = totalDays > 0
                    ? Math.Round((double)daysElapsed / totalDays * 100, 1)
                    : 0;

                // Deadlines
                var deadlines = cyclesByTerm.TryGetValue(term.TermId, out var termCycles)
                    ? termCycles.Select(ec =>
                    {
                        var daysUntil = (int)Math.Ceiling((ec.EndDate - nowUtc).TotalDays);
                        return new DeadlineInfo
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
                    : new List<DeadlineInfo>();

                return new ActiveTermTimelineResponse
                {
                    TermId = term.TermId,
                    TermName = term.Name,
                    UniversityId = term.UniversityId,
                    UniversityName = term.University.Name,
                    StartDate = term.StartDate,
                    EndDate = term.EndDate,
                    Status = TermDisplayStatus.Active,
                    TotalDays = totalDays,
                    DaysElapsed = daysElapsed,
                    DaysRemaining = daysRemaining,
                    ProgressPercent = progressPercent,
                    Deadlines = deadlines
                };
            }).ToList();

            var response = new GetActiveTermsForEnterpriseResponse { Terms = termResponses };

            await _cacheService.SetAsync(cacheKey, response, EnterpriseCacheKeys.Expiration.ActiveTerms, cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.ActiveTerms.LogRetrieved),
                termResponses.Count, enterpriseId, isMentor);

            return Result<GetActiveTermsForEnterpriseResponse>.Success(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ActiveTerms.LogError), _currentUserService.UserId);
            return Result<GetActiveTermsForEnterpriseResponse>.Failure(
                _messageService.GetMessage(MessageKeys.ActiveTerms.SystemError), ResultErrorType.InternalServerError);
        }
    }
}
