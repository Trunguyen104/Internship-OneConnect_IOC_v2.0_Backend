using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Epics.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Epics.Queries.GetEpicById;

public class GetEpicByIdHandler : IRequestHandler<GetEpicByIdQuery, Result<GetEpicByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;

    public GetEpicByIdHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
        _messageService = messageService;
    }

    public async Task<Result<GetEpicByIdResponse>> Handle(
        GetEpicByIdQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = EpicCacheKeys.Epic(request.ProjectId, request.EpicId);

        var cachedResult = await _cacheService.GetAsync<GetEpicByIdResponse>(cacheKey, cancellationToken);
        if (cachedResult is not null)
            return Result<GetEpicByIdResponse>.Success(cachedResult);

        var epic = await _unitOfWork.Repository<WorkItem>().Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WorkItemId == request.EpicId && w.ProjectId == request.ProjectId && w.Type == WorkItemType.Epic, cancellationToken);

        if (epic is null)
            return Result<GetEpicByIdResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Epic.NotFound), ResultErrorType.NotFound);

        var response = _mapper.Map<GetEpicByIdResponse>(epic);

        await _cacheService.SetAsync(cacheKey, response, EpicCacheKeys.Expiration.Epic, cancellationToken);

        return Result<GetEpicByIdResponse>.Success(response);
    }
}
