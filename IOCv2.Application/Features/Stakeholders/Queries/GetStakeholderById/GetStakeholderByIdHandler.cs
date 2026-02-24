using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Stakeholders.DTOs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById
{
    public class GetStakeholderByIdHandler : IRequestHandler<GetStakeholderByIdQuery, Result<StakeholderDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public GetStakeholderByIdHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<StakeholderDto>> Handle(GetStakeholderByIdQuery request, CancellationToken cancellationToken)
        {
            var stakeholder = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .AsNoTracking()
                .ProjectTo<StakeholderDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (stakeholder == null)
                return Result<StakeholderDto>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.NotFound));

            return Result<StakeholderDto>.Success(stakeholder);
        }
    }
}

