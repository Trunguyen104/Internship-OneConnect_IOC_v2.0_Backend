using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.WorkItems.Commands.CreateWorkItem;

public class CreateWorkItemHandler : IRequestHandler<CreateWorkItemCommand, Result<CreateWorkItemResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;

    public CreateWorkItemHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageService = messageService;
    }

    public async Task<Result<CreateWorkItemResponse>> Handle(
        CreateWorkItemCommand request, CancellationToken cancellationToken)
    {
        // Parse Type
        if (!Enum.TryParse<WorkItemType>(request.Type, ignoreCase: true, out var type))
            return Result<CreateWorkItemResponse>.Failure(
                _messageService.GetMessage(MessageKeys.WorkItem.TypeInvalid), ResultErrorType.BadRequest);

        // Parse Priority (optional)
        Priority? priority = null;
        if (!string.IsNullOrWhiteSpace(request.Priority))
        {
            if (!Enum.TryParse<Priority>(request.Priority, ignoreCase: true, out var parsedPriority))
                return Result<CreateWorkItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.WorkItem.PriorityInvalid), ResultErrorType.BadRequest);
            priority = parsedPriority;
        }

        // Validate SprintId if provided
        if (request.SprintId.HasValue)
        {
            var sprintExists = await _unitOfWork.Repository<Sprint>()
                .ExistsAsync(s => s.SprintId == request.SprintId.Value, cancellationToken);
            if (!sprintExists)
                return Result<CreateWorkItemResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Sprint.NotFound), ResultErrorType.NotFound);
        }

        // Lấy BacklogOrder nhỏ nhất hiện tại (thẻ ở ngay vị trí đầu tiên)
        var minBacklogOrder = await _unitOfWork.Repository<WorkItem>()
            .Query()
            .Where(w => w.ProjectId == request.ProjectId)
            .Select(w => (float?)w.BacklogOrder)
            .MinAsync(cancellationToken) ?? 2000f; // Nếu chưa có thẻ nào, khởi tạo số 2000

        var workItem = new WorkItem
        {
            WorkItemId = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            ParentId = request.ParentId,
            Type = type,
            Title = request.Title,
            Description = request.Description,
            Priority = priority,
            Status = WorkItemStatus.Todo,
            StoryPoint = request.StoryPoint,
            AssigneeId = request.AssigneeId,
            DueDate = request.DueDate,
            BacklogOrder = minBacklogOrder - 1000f
        };

        await _unitOfWork.Repository<WorkItem>().AddAsync(workItem, cancellationToken);

        // If SprintId provided → assign to Sprint with BoardOrder at top
        if (request.SprintId.HasValue)
        {
            var minBoardOrder = await _unitOfWork.Repository<SprintWorkItem>()
                .Query()
                .Where(swi => swi.SprintId == request.SprintId.Value)
                .Select(swi => (float?)swi.BoardOrder)
                .MinAsync(cancellationToken) ?? 2000f; // Tương tự, 2000 nếu sprint rỗng

            var sprintWorkItem = new SprintWorkItem
            {
                SprintId = request.SprintId.Value,
                WorkItemId = workItem.WorkItemId,
                BoardOrder = minBoardOrder - 1000f
            };
            await _unitOfWork.Repository<SprintWorkItem>().AddAsync(sprintWorkItem, cancellationToken);
        }

        await _unitOfWork.SaveChangeAsync(cancellationToken);

        var response = _mapper.Map<CreateWorkItemResponse>(workItem);
        return Result<CreateWorkItemResponse>.Success(response);
    }
}
