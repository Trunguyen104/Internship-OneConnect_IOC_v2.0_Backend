using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.WorkItems.Commands.DeleteWorkItem;

public class DeleteWorkItemHandler : IRequestHandler<DeleteWorkItemCommand, Result<DeleteWorkItemResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessageService _messageService;

    public DeleteWorkItemHandler(IUnitOfWork unitOfWork, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
    }

    public async Task<Result<DeleteWorkItemResponse>> Handle(
        DeleteWorkItemCommand request, CancellationToken cancellationToken)
    {
        var workItem = await _unitOfWork.Repository<WorkItem>()
            .Query()
            .FirstOrDefaultAsync(w => w.WorkItemId == request.WorkItemId && w.ProjectId == request.ProjectId, cancellationToken);

        if (workItem is null)
            return Result<DeleteWorkItemResponse>.Failure(
                _messageService.GetMessage(MessageKeys.WorkItem.NotFound), ResultErrorType.NotFound);

        await _unitOfWork.Repository<WorkItem>().DeleteAsync(workItem, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<DeleteWorkItemResponse>.Success(new DeleteWorkItemResponse
        {
            WorkItemId = workItem.WorkItemId,
            Title = workItem.Title
        });
    }
}
