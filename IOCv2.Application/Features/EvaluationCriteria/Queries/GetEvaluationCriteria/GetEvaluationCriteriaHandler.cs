using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.EvaluationCriteria.Queries.GetEvaluationCriteria;

public class GetEvaluationCriteriaHandler
    : IRequestHandler<GetEvaluationCriteriaQuery, Result<List<GetEvaluationCriteriaResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEvaluationCriteriaHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<GetEvaluationCriteriaResponse>>> Handle(
        GetEvaluationCriteriaQuery request, CancellationToken cancellationToken)
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
}
