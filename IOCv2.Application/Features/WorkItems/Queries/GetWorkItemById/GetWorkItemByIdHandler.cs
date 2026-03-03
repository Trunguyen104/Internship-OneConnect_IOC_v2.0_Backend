using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Constants;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.WorkItems.Queries.GetWorkItemById;

public class GetWorkItemByIdHandler : IRequestHandler<GetWorkItemByIdQuery, Result<GetWorkItemByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;

    public GetWorkItemByIdHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageService = messageService;
    }

    public async Task<Result<GetWorkItemByIdResponse>> Handle(
        GetWorkItemByIdQuery request, CancellationToken cancellationToken)
    {
        var workItem = await _unitOfWork.Repository<WorkItem>()
            .Query()
            .AsNoTracking()
            .Include(w => w.Assignee)
                .ThenInclude(s => s!.User)
            .FirstOrDefaultAsync(w => w.WorkItemId == request.WorkItemId && w.ProjectId == request.ProjectId, cancellationToken);

        if (workItem is null)
        {
            return Result<GetWorkItemByIdResponse>.NotFound(
                _messageService.GetMessage(MessageKeys.Error.WorkItemNotFound, request.WorkItemId));
        }

        return Result<GetWorkItemByIdResponse>.Success(_mapper.Map<GetWorkItemByIdResponse>(workItem));
    }
}
