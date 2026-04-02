using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipPhases.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
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
            request.Status.HasValue ? request.Status.Value.ToString() : "All",
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
            request.IncludeEnded,
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

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = _unitOfWork.Repository<InternshipPhase>().Query()
            .Include(p => p.Enterprise)
            .Include(p => p.InternshipGroups)
                .ThenInclude(g => g.Members)
            .Where(p => p.DeletedAt == null)
            .AsNoTracking();

        if (targetEnterpriseId.HasValue)
        {
            query = query.Where(p => p.EnterpriseId == targetEnterpriseId.Value);
        }

        if (request.Status.HasValue)
        {
            query = request.Status.Value switch
            {
                InternshipPhaseLifecycleStatus.Upcoming => query.Where(p => p.StartDate > today),
                InternshipPhaseLifecycleStatus.Active => query.Where(p => p.StartDate <= today && p.EndDate >= today),
                InternshipPhaseLifecycleStatus.Ended => query.Where(p => p.EndDate < today),
                _ => query
            };
        }
        else if (!request.IncludeEnded)
        {
            query = query.Where(p => p.EndDate >= today);
        }

        query = query.OrderByDescending(p => p.StartDate);

        var totalCount = await query.CountAsync(cancellationToken);

        var phases = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var items = phases.Select(p =>
        {
            var placedCount = p.InternshipGroups
                .Where(g => g.DeletedAt == null)
                .SelectMany(g => g.Members)
                .Select(m => m.StudentId)
                .Distinct()
                .Count();

            return new GetInternshipPhasesResponse
            {
                PhaseId = p.PhaseId,
                EnterpriseId = p.EnterpriseId,
                EnterpriseName = p.Enterprise?.Name ?? string.Empty,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                MajorFields = p.MajorFields,
                Capacity = p.Capacity,
                RemainingCapacity = Math.Max(p.Capacity - placedCount, 0),
                JobPostingCount = 0,
                Description = p.Description,
                Status = p.GetLifecycleStatus(today),
                GroupCount = p.InternshipGroups.Count(g => g.DeletedAt == null),
                CreatedAt = p.CreatedAt
            };
        }).ToList();

        var result = new PaginatedResult<GetInternshipPhasesResponse>(items, totalCount, request.PageNumber, request.PageSize);

        await _cacheService.SetAsync(cacheKey, result, InternshipPhaseCacheKeys.Expiration.PhaseList, cancellationToken);

        _logger.LogInformation(
            _messageService.GetMessage(MessageKeys.InternshipPhase.LogListSuccess),
            targetEnterpriseId?.ToString() ?? "All", items.Count, totalCount, request.PageNumber);

        return Result<PaginatedResult<GetInternshipPhasesResponse>>.Success(result);
    }
}
