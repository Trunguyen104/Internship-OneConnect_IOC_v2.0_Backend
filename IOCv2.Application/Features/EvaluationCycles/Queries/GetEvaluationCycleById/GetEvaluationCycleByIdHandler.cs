using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.EvaluationCycles.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycleById;

public class GetEvaluationCycleByIdHandler
    : IRequestHandler<GetEvaluationCycleByIdQuery, Result<GetEvaluationCycleByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetEvaluationCycleByIdHandler> _logger;
    private readonly ICacheService _cacheService;

    public GetEvaluationCycleByIdHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<GetEvaluationCycleByIdHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<GetEvaluationCycleByIdResponse>> Handle(
        GetEvaluationCycleByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting EvaluationCycle {CycleId}", request.CycleId);

            var cacheKey = EvaluationCycleCacheKeys.Cycle(request.CycleId);
            var cached = await _cacheService.GetAsync<GetEvaluationCycleByIdResponse>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<GetEvaluationCycleByIdResponse>.Success(cached);

            var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
                .AsNoTracking()
                .Include(c => c.InternshipPhase)
                .Include(c => c.Criteria)
                .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle is null)
        {
            _logger.LogWarning("EvaluationCycle {CycleId} not found", request.CycleId);
            return Result<GetEvaluationCycleByIdResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.NotFound),
                ResultErrorType.NotFound);
        }

        var response = new GetEvaluationCycleByIdResponse
        {
            CycleId = cycle.CycleId,
            PhaseId = cycle.PhaseId,
            PhaseName = cycle.InternshipPhase?.Name ?? string.Empty,
            Name = cycle.Name,
            StartDate = cycle.StartDate,
            EndDate = cycle.EndDate,
            Status = cycle.Status,

            CreatedAt = cycle.CreatedAt,
            UpdatedAt = cycle.UpdatedAt,
            Criteria = cycle.Criteria
                .Where(c => c.DeletedAt == null)
                .Select(c => new CriteriaDto
                {
                    CriteriaId = c.CriteriaId,
                    Name = c.Name,
                    Description = c.Description,
                    MaxScore = c.MaxScore,
                    Weight = c.Weight
                })
                .ToList()
        };

            await _cacheService.SetAsync(cacheKey, response, EvaluationCycleCacheKeys.Expiration.Cycle, cancellationToken);

            return Result<GetEvaluationCycleByIdResponse>.Success(response);
    }
}
