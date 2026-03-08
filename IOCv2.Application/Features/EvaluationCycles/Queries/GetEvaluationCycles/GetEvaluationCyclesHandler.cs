using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycles;

public class GetEvaluationCyclesHandler
    : IRequestHandler<GetEvaluationCyclesQuery, Result<List<GetEvaluationCyclesResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetEvaluationCyclesHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<GetEvaluationCyclesResponse>>> Handle(
        GetEvaluationCyclesQuery request, CancellationToken cancellationToken)
    {
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

        return Result<List<GetEvaluationCyclesResponse>>.Success(cycles);
    }
}
