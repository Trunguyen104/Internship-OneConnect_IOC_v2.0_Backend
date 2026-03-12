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
        private readonly ICurrentUserService _currentUserService;

        public GetStakeholderByIdHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMessageService messageService,
            ILogger<GetStakeholderByIdHandler> logger,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<GetStakeholderByIdResponse>> Handle(GetStakeholderByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting stakeholder {Id}", request.StakeholderId);

            try
            {

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

            // Security: Ownership check (FFA-SEC)
            var currentUserIdStr = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserIdStr) || !Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Result<GetStakeholderByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            var userRole = _currentUserService.Role;
            if (userRole != "SchoolAdmin" && userRole != "SuperAdmin" && userRole != "Moderator")
            {
                var isAuthorized = await _unitOfWork.Repository<InternshipGroup>()
                    .Query()
                    .AnyAsync(g => g.InternshipId == request.InternshipId &&
                        (
                            (g.Mentor != null && g.Mentor.UserId == currentUserId) ||
                            g.Members.Any(m => m.Student.UserId == currentUserId)
                        ), cancellationToken);

                if (!isAuthorized)
                {
                    _logger.LogWarning("User {UserId} attempted to get stakeholder in internship {InternshipId} without permission", currentUserId, request.InternshipId);
                    return Result<GetStakeholderByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
                }
            }

            _logger.LogInformation("Successfully retrieved stakeholder {Id}", request.StakeholderId);
            var response = _mapper.Map<GetStakeholderByIdResponse>(stakeholder);
            return Result<GetStakeholderByIdResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting stakeholder {Id}", request.StakeholderId);
                return Result<GetStakeholderByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}
