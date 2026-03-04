using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholderById
{
    public class GetStakeholderByIdHandler : IRequestHandler<GetStakeholderByIdQuery, Result<GetStakeholderByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetStakeholderByIdHandler> _logger;

        public GetStakeholderByIdHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMessageService messageService,
            ILogger<GetStakeholderByIdHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetStakeholderByIdResponse>> Handle(GetStakeholderByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting stakeholder {Id}", request.Id);

            var stakeholder = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (stakeholder == null)
            {
                _logger.LogWarning("Stakeholder {Id} not found", request.Id);
                return Result<GetStakeholderByIdResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.NotFound));
            }

            // TODO: Ownership check

            _logger.LogInformation("Successfully retrieved stakeholder {Id}", request.Id);
            var response = _mapper.Map<GetStakeholderByIdResponse>(stakeholder);
            return Result<GetStakeholderByIdResponse>.Success(response);
        }
    }
}
