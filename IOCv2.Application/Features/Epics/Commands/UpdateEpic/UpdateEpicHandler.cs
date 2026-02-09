using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Epics.Commands.UpdateEpic;

public class UpdateEpicHandler : IRequestHandler<UpdateEpicCommand, Result<UpdateEpicResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    
    public UpdateEpicHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    
    public async Task<Result<UpdateEpicResponse>> Handle(UpdateEpicCommand request, CancellationToken cancellationToken)
    {
        // Find Epic
        var epic = await _unitOfWork.Repository<WorkItem>()
            .FindAsync(w => w.WorkItemId == request.EpicId && w.Type == WorkItemType.Epic, cancellationToken);
        
        var epicEntity = epic.FirstOrDefault();
        
        if (epicEntity == null)
        {
            return Result<UpdateEpicResponse>.NotFound("Epic not found.");
        }
        
        // Update only Title and Description (Epic-specific fields)
        epicEntity.Title = request.Name;
        epicEntity.Description = request.Description;
        epicEntity.UpdatedAt = DateTime.UtcNow;
        
        await _unitOfWork.Repository<WorkItem>().UpdateAsync(epicEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        var response = _mapper.Map<UpdateEpicResponse>(epicEntity);
        return Result<UpdateEpicResponse>.Success(response);
    }
}
