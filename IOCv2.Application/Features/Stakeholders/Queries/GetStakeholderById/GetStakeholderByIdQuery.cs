using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Stakeholders.DTOs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById;

public record GetStakeholderByIdQuery(Guid Id) : IRequest<Result<StakeholderDto>>;

public class GetStakeholderByIdQueryHandler 
    : IRequestHandler<GetStakeholderByIdQuery, Result<StakeholderDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<Resources.ErrorMessages> _localizer;

    public GetStakeholderByIdQueryHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        IStringLocalizer<Resources.ErrorMessages> localizer)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _localizer = localizer;
    }

    public async Task<Result<StakeholderDto>> Handle(
        GetStakeholderByIdQuery request, 
        CancellationToken cancellationToken)
    {
        var stakeholder = await _unitOfWork.Repository<Stakeholder>()
            .Query()
            .Where(s => s.Id == request.Id && s.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken);

        if (stakeholder == null)
        {
            return Result<StakeholderDto>.NotFound(_localizer["Stakeholder.NotFound"]);
        }

        var dto = _mapper.Map<StakeholderDto>(stakeholder);
        return Result<StakeholderDto>.Success(dto);
    }
}

