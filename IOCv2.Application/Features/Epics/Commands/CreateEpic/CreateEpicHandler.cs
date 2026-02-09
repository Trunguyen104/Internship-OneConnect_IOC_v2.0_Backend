using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;

namespace IOCv2.Application.Features.Epics.Commands.CreateEpic;

public class CreateEpicHandler : IRequestHandler<CreateEpicCommand, Result<CreateEpicResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    
    public CreateEpicHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    
    public async Task<Result<CreateEpicResponse>> Handle(CreateEpicCommand request, CancellationToken cancellationToken)
    {
        // Create Epic WorkItem
        var epic = new WorkItem
        {
            WorkItemId = Guid.NewGuid(),
            ProjectId = request.ProjectId,
            ParentId = null, // Epic is always root level
            Type = WorkItemType.Epic,
            Title = request.Name,
            Description = request.Description,
            
            // Epic does not use these fields - set to null/default
            Priority = null,
            Status = null,
            BacklogOrder = 0,
            StoryPoint = null,
            StartDate = null,
            DueDate = null,
            OriginalEstimate = null,
            RemainingWork = null
        };
        
        await _unitOfWork.Repository<WorkItem>().AddAsync(epic, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        var response = _mapper.Map<CreateEpicResponse>(epic);
        return Result<CreateEpicResponse>.Success(response);
    }
}
