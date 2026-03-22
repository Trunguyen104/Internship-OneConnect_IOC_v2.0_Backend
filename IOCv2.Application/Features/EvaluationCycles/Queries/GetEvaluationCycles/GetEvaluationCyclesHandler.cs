using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.EvaluationCycles.Common;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycles;

public class GetEvaluationCyclesHandler
    : IRequestHandler<GetEvaluationCyclesQuery, Result<List<GetEvaluationCyclesResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetEvaluationCyclesHandler> _logger;
    private readonly ICacheService _cacheService;

    public GetEvaluationCyclesHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<GetEvaluationCyclesHandler> logger,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<List<GetEvaluationCyclesResponse>>> Handle(
        GetEvaluationCyclesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting EvaluationCycles for Term {TermId}", request.TermId);

        try
        {
            var cacheKey = EvaluationCycleCacheKeys.CycleList(request.TermId);
            var cached = await _cacheService.GetAsync<List<GetEvaluationCyclesResponse>>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<List<GetEvaluationCyclesResponse>>.Success(cached);

            var cycles = await _unitOfWork.Repository<EvaluationCycle>().Query()
                .AsNoTracking()
                .Where(c => c.TermId == request.TermId)
                .OrderBy(c => c.StartDate)
            .Select(c => new GetEvaluationCyclesResponse
            {
                CycleId = c.CycleId,
                TermId = c.TermId,
                Name = c.Name,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                Status = c.Status,

                CriteriaCount = c.Criteria.Count(cr => cr.DeletedAt == null),
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(cancellationToken);

            await _cacheService.SetAsync(cacheKey, cycles, EvaluationCycleCacheKeys.Expiration.CycleList, cancellationToken);

            return Result<List<GetEvaluationCyclesResponse>>.Success(cycles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting EvaluationCycles for Term {TermId}", request.TermId);
            return Result<List<GetEvaluationCyclesResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}
