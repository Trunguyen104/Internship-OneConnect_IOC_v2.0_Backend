using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.EvaluationCycles.Commands.CreateEvaluationCycle;

public class CreateEvaluationCycleHandler
    : IRequestHandler<CreateEvaluationCycleCommand, Result<CreateEvaluationCycleResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public CreateEvaluationCycleHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<CreateEvaluationCycleResponse>> Handle(
        CreateEvaluationCycleCommand request, CancellationToken cancellationToken)
    {
        var termExists = await _unitOfWork.Repository<Term>().Query()
            .AnyAsync(t => t.TermId == request.TermId, cancellationToken);

        if (!termExists)
            return Result<CreateEvaluationCycleResponse>.Failure(
                _messageService.GetMessage(MessageKeys.EvaluationCycle.TermNotFound),
                ResultErrorType.NotFound);

        var cycle = new EvaluationCycle
        {
            CycleId = Guid.NewGuid(),
            TermId = request.TermId,
            Name = request.Name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = EvaluationCycleStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Repository<EvaluationCycle>().AddAsync(cycle, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<CreateEvaluationCycleResponse>.Success(new CreateEvaluationCycleResponse
        {
            CycleId = cycle.CycleId,
            TermId = cycle.TermId,
            Name = cycle.Name,
            StartDate = cycle.StartDate,
            EndDate = cycle.EndDate,
            Status = cycle.Status.ToString(),
            CreatedAt = cycle.CreatedAt
        });
    }
}
