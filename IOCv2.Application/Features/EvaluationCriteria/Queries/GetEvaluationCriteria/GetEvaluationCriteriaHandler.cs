using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.EvaluationCriteria.Queries.GetEvaluationCriteria;

public class GetEvaluationCriteriaHandler
    : IRequestHandler<GetEvaluationCriteriaQuery, Result<List<GetEvaluationCriteriaResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;
    private readonly ILogger<GetEvaluationCriteriaHandler> _logger;

    public GetEvaluationCriteriaHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<GetEvaluationCriteriaHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
    }

    public async Task<Result<List<GetEvaluationCriteriaResponse>>> Handle(
        GetEvaluationCriteriaQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting EvaluationCriteria for Cycle {CycleId}", request.CycleId);

        try
        {
            var criteria = await _unitOfWork.Repository<Domain.Entities.EvaluationCriteria>().Query()
                .AsNoTracking()
                .Where(c => c.CycleId == request.CycleId)
                .OrderBy(c => c.CreatedAt)
            .Select(c => new GetEvaluationCriteriaResponse
            {
                CriteriaId = c.CriteriaId,
                CycleId = c.CycleId,
                Name = c.Name,
                Description = c.Description,
                MaxScore = c.MaxScore,
                Weight = c.Weight,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            })
            .ToListAsync(cancellationToken);

            return Result<List<GetEvaluationCriteriaResponse>>.Success(criteria);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while getting EvaluationCriteria for Cycle {CycleId}", request.CycleId);
            return Result<List<GetEvaluationCriteriaResponse>>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
        }
    }
}
