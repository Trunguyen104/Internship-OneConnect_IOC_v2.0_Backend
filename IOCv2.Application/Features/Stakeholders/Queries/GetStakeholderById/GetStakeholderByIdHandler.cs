using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Stakeholders.Common;
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
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public GetStakeholderByIdHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetStakeholderByIdHandler> logger,
            ICurrentUserService currentUserService,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
        }

        public async Task<Result<GetStakeholderByIdResponse>> Handle(GetStakeholderByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting stakeholder {Id}", request.StakeholderId);


            var stakeholder = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.StakeholderId && s.InternshipId == request.InternshipId, cancellationToken);

            if (stakeholder == null)
            {
                _logger.LogWarning("Stakeholder {Id} not found in internship {InternshipId}", request.StakeholderId, request.InternshipId);
                return Result<GetStakeholderByIdResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.NotFound));
            }

            var authError = StakeholderAccessGuard.EnsureAuthenticated<GetStakeholderByIdResponse>(_currentUserService, _messageService);
            if (authError is not null)
            {
                return authError;
            }

            var accessError = await StakeholderAccessGuard.EnsureInternshipAccessAsync<GetStakeholderByIdResponse>(
                _unitOfWork,
                _messageService,
                _currentUserService,
                request.InternshipId,
                cancellationToken);

            if (accessError is not null)
            {
                _logger.LogWarning("User {UserId} attempted to get stakeholder in internship {InternshipId} without permission", _currentUserService.UserId, request.InternshipId);
                return accessError;
            }

            var cacheKey = StakeholderCacheKeys.Stakeholder(request.StakeholderId);
            var cached = await _cacheService.GetAsync<GetStakeholderByIdResponse>(cacheKey, cancellationToken);
            if (cached is not null)
                return Result<GetStakeholderByIdResponse>.Success(cached);

            _logger.LogInformation("Successfully retrieved stakeholder {Id}", request.StakeholderId);
            var response = _mapper.Map<GetStakeholderByIdResponse>(stakeholder);
            await _cacheService.SetAsync(cacheKey, response, StakeholderCacheKeys.Expiration.Stakeholder, cancellationToken);
            return Result<GetStakeholderByIdResponse>.Success(response);

        }
    }
}
