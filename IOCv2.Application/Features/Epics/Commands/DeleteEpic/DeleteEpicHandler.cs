using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Resources;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Epics.Commands.DeleteEpic;

public class DeleteEpicHandler : IRequestHandler<DeleteEpicCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    
    public DeleteEpicHandler(IUnitOfWork unitOfWork, IStringLocalizer<ErrorMessages> localizer)
    {
        _unitOfWork = unitOfWork;
        _localizer = localizer;
    }
    
    public async Task<Result> Handle(DeleteEpicCommand request, CancellationToken cancellationToken)
    {
        // Find Epic with children
        var epic = await _unitOfWork.Repository<WorkItem>()
            .FindAsync(w => w.WorkItemId == request.EpicId && w.Type == WorkItemType.Epic, cancellationToken);
        
        var epicEntity = epic.FirstOrDefault();
        
        if (epicEntity == null)
        {
            return Result.NotFound(_localizer["Epic.NotFound"]);
        }
        
        // Check if Epic has children
        var childrenCount = await _unitOfWork.Repository<WorkItem>()
            .CountAsync(w => w.ParentId == request.EpicId, cancellationToken);
        
        if (childrenCount > 0)
        {
            return Result.Failure(
                _localizer["Epic.CannotDeleteWithChildren"],
                ResultErrorType.BadRequest
            );
        }
        
        // Soft delete - set DeletedAt
        epicEntity.DeletedAt = DateTime.UtcNow;
        
        await _unitOfWork.Repository<WorkItem>().UpdateAsync(epicEntity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}
