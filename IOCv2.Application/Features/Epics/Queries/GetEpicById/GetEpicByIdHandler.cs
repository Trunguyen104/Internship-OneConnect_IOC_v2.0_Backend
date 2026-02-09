using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Resources;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Epics.Queries.GetEpicById;

public class GetEpicByIdHandler : IRequestHandler<GetEpicByIdQuery, Result<GetEpicByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<ErrorMessages> _localizer;
    
    public GetEpicByIdHandler(IUnitOfWork unitOfWork, IMapper mapper, IStringLocalizer<ErrorMessages> localizer)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _localizer = localizer;
    }
    
    public async Task<Result<GetEpicByIdResponse>> Handle(GetEpicByIdQuery request, CancellationToken cancellationToken)
    {
        var epic = await _unitOfWork.Repository<WorkItem>()
            .FindAsync(w => w.WorkItemId == request.EpicId && w.Type == WorkItemType.Epic, cancellationToken);
        
        var epicEntity = epic.FirstOrDefault();
        
        if (epicEntity == null)
        {
            return Result<GetEpicByIdResponse>.NotFound(_localizer["Epic.NotFound"]);
        }
        
        var response = _mapper.Map<GetEpicByIdResponse>(epicEntity);
        return Result<GetEpicByIdResponse>.Success(response);
    }
}
