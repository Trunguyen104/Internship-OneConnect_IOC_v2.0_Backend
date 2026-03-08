using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.EvaluationCycles.Queries.GetEvaluationCycleById;

public class GetEvaluationCycleByIdHandler
    : IRequestHandler<GetEvaluationCycleByIdQuery, Result<GetEvaluationCycleByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public GetEvaluationCycleByIdHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<GetEvaluationCycleByIdResponse>> Handle(
        GetEvaluationCycleByIdQuery request, CancellationToken cancellationToken)
    {
        var cycle = await _unitOfWork.Repository<EvaluationCycle>().Query()
            .AsNoTracking()
            .Include(c => c.Term)
            .Include(c => c.Criteria)
            .FirstOrDefaultAsync(c => c.CycleId == request.CycleId, cancellationToken);

        if (cycle is null)
            return Result<GetEvaluationCycleByIdResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.NotFound),
                ResultErrorType.NotFound);

        var response = new GetEvaluationCycleByIdResponse
        {
            CycleId = cycle.CycleId,
            TermId = cycle.TermId,
            TermName = cycle.Term.Name,
            Name = cycle.Name,
            StartDate = cycle.StartDate,
            EndDate = cycle.EndDate,
            Status = cycle.Status.ToString(),
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

        return Result<GetEvaluationCycleByIdResponse>.Success(response);
    }
}
