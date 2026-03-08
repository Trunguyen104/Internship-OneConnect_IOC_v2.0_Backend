using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.WorkItems.Commands.UpdateWorkItem;

public class UpdateWorkItemHandler : IRequestHandler<UpdateWorkItemCommand, Result<UpdateWorkItemResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;

    public UpdateWorkItemHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageService = messageService;
    }

    public async Task<Result<UpdateWorkItemResponse>> Handle(
        UpdateWorkItemCommand request, CancellationToken cancellationToken)
    {
        var workItem = await _unitOfWork.Repository<WorkItem>()
            .Query()
            .FirstOrDefaultAsync(w => w.WorkItemId == request.WorkItemId && w.ProjectId == request.ProjectId, cancellationToken);

        if (workItem is null)
            return Result<UpdateWorkItemResponse>.Failure(
                _messageService.GetMessage(MessageKeys.WorkItem.NotFound), ResultErrorType.NotFound);

        if (request.Title is not null)
            workItem.Title = request.Title;

        if (request.Description is not null)
            workItem.Description = request.Description;

        if (request.StoryPoint.HasValue)
            workItem.StoryPoint = request.StoryPoint;

        if (request.AssigneeId.HasValue)
            workItem.AssigneeId = request.AssigneeId;

        if (request.DueDate.HasValue)
            workItem.DueDate = request.DueDate;

        if (request.Priority.HasValue)
            workItem.Priority = request.Priority.Value;

        if (request.Status.HasValue)
            workItem.Status = request.Status.Value;

        workItem.UpdatedAt = DateTime.UtcNow;
        await _unitOfWork.Repository<WorkItem>().UpdateAsync(workItem, cancellationToken);
        await _unitOfWork.SaveChangeAsync(cancellationToken);

        return Result<UpdateWorkItemResponse>.Success(_mapper.Map<UpdateWorkItemResponse>(workItem));
    }
}
