using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipPhases.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using System;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipPhases.Queries.GetInternshipPhases;

public class GetInternshipPhasesHandler
    : IRequestHandler<GetInternshipPhasesQuery, Result<PaginatedResult<GetInternshipPhasesResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IMessageService _messageService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<GetInternshipPhasesHandler> _logger;

    public GetInternshipPhasesHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IMessageService messageService,
        ICacheService cacheService,
        ILogger<GetInternshipPhasesHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _messageService = messageService;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<GetInternshipPhasesResponse>>> Handle(
        GetInternshipPhasesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogGettingList),
            request.EnterpriseId?.ToString() ?? "All",
            string.IsNullOrWhiteSpace(request.Status) ? "All" : request.Status,
            request.PageNumber);

        var role = _currentUserService.Role;
        Guid? targetEnterpriseId = request.EnterpriseId;

        // ── BUG-09 FIX: Ownership check; return Unauthorized when UserId is invalid ──
        if (role != "SuperAdmin" && role != "SchoolAdmin")
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<PaginatedResult<GetInternshipPhasesResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            // BUG-06 FIX: null enterpriseUser → Forbidden (previously fell through with no restriction)
            if (enterpriseUser == null)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                    currentUserId, request.EnterpriseId, Guid.Empty);
                return Result<PaginatedResult<GetInternshipPhasesResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            if (targetEnterpriseId.HasValue && enterpriseUser.EnterpriseId != targetEnterpriseId.Value)
            {
                _logger.LogWarning(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.LogOwnershipDenied),
                    currentUserId, request.EnterpriseId, enterpriseUser.EnterpriseId);
                return Result<PaginatedResult<GetInternshipPhasesResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipPhase.NotYourEnterprise),
                    ResultErrorType.Forbidden);
            }
            
            targetEnterpriseId = enterpriseUser.EnterpriseId;
        }

        var cacheKey = InternshipPhaseCacheKeys.PhaseList(
            targetEnterpriseId,
            request.Status,
            request.PageNumber,
            request.PageSize);

        var cached = await _cacheService.GetAsync<PaginatedResult<GetInternshipPhasesResponse>>(cacheKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogInformation(
                _messageService.GetMessage(MessageKeys.InternshipPhase.LogListFromCache),
                targetEnterpriseId);
            return Result<PaginatedResult<GetInternshipPhasesResponse>>.Success(cached);
        }

        var query = _unitOfWork.Repository<InternshipPhase>().Query()
            .Include(p => p.Enterprise)
            .Include(p => p.InternshipGroups)
            .Include(p => p.Jobs)
            .Where(p => p.DeletedAt == null)
            .AsNoTracking();

        if (targetEnterpriseId.HasValue)
        {
            query = query.Where(p => p.EnterpriseId == targetEnterpriseId.Value);
        }

        query = query.OrderByDescending(p => p.StartDate);

        var phasesForFilter = await query.ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var filtered = phasesForFilter.Where(p =>
        {
            var lifecycle = ToLifecycleStatus(p, today);

            if (!request.IncludeEnded && lifecycle == InternshipPhaseLifecycleStatus.Ended)
                return false;

            if (string.IsNullOrWhiteSpace(request.Status))
                return true;

            return lifecycle.ToString().Equals(request.Status, StringComparison.OrdinalIgnoreCase);
        }).ToList();

        var totalCount = filtered.Count;

        var phases = filtered
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var phaseIds = phases.Select(p => p.PhaseId).ToList();
        var placedLookup = phaseIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _unitOfWork.Repository<InternshipApplication>().Query()
                .AsNoTracking()
                .Where(a => phaseIds.Contains(a.TermId)
                            && a.Status == InternshipApplicationStatus.Placed
                            && a.DeletedAt == null)
                .GroupBy(a => a.TermId)
                .Select(g => new { PhaseId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PhaseId, x => x.Count, cancellationToken);

        var items = phases.Select(p => new GetInternshipPhasesResponse
        {
            PhaseId = p.PhaseId,
            EnterpriseId = p.EnterpriseId,
            EnterpriseName = p.Enterprise?.Name ?? string.Empty,
            Name = p.Name,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            MajorFields = p.MajorFields,
            Capacity = p.Capacity,
            RemainingCapacity = Math.Max(p.Capacity - placedLookup.GetValueOrDefault(p.PhaseId), 0),
            Description = p.Description,
            Status = ToLifecycleStatus(p, today),
            JobPostingCount = p.Jobs.Count(j => j.DeletedAt == null),
            GroupCount = p.InternshipGroups.Count(g => g.DeletedAt == null),
            CreatedAt = p.CreatedAt
        }).ToList();

        var result = new PaginatedResult<GetInternshipPhasesResponse>(items, totalCount, request.PageNumber, request.PageSize);

        await _cacheService.SetAsync(cacheKey, result, InternshipPhaseCacheKeys.Expiration.PhaseList, cancellationToken);

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogListSuccess),
            targetEnterpriseId?.ToString() ?? "All", items.Count, totalCount, request.PageNumber);

        return Result<PaginatedResult<GetInternshipPhasesResponse>>.Success(result);
    }

    private static InternshipPhaseLifecycleStatus ToLifecycleStatus(InternshipPhase phase, DateOnly today)
    {
        if (phase.IsUpcoming(today)) return InternshipPhaseLifecycleStatus.Upcoming;
        if (phase.IsActive(today)) return InternshipPhaseLifecycleStatus.Active;
        return InternshipPhaseLifecycleStatus.Ended;
    }
}
