﻿using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Stakeholders.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder
{
    public class UpdateStakeholderHandler : IRequestHandler<UpdateStakeholderCommand, Result<UpdateStakeholderResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<UpdateStakeholderHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public UpdateStakeholderHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<UpdateStakeholderHandler> logger,
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

        public async Task<Result<UpdateStakeholderResponse>> Handle(UpdateStakeholderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating stakeholder {Id}", request.StakeholderId);

            // Find stakeholder
            var stakeholder = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .FirstOrDefaultAsync(s => s.Id == request.StakeholderId && s.InternshipId == request.InternshipId, cancellationToken);

            if (stakeholder == null)
            {
                _logger.LogWarning("Stakeholder {Id} not found in internship {InternshipId}", request.StakeholderId, request.InternshipId);
                return Result<UpdateStakeholderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.NotFound));
            }

            // Security: Ownership check (FFA-SEC)
            var currentUserIdStr = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserIdStr) || !Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Result<UpdateStakeholderResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
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
                    _logger.LogWarning("User {UserId} attempted to update stakeholder in internship {InternshipId} without permission", currentUserId, request.InternshipId);
                    return Result<UpdateStakeholderResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
                }
            }

            // Partial update values
            var name = request.Name?.Trim() ?? stakeholder.Name;
            var type = request.Type ?? stakeholder.Type;
            var email = request.Email?.Trim() ?? stakeholder.Email;
            var role = request.Role?.Trim() ?? stakeholder.Role;
            var description = request.Description?.Trim() ?? stakeholder.Description;
            var phoneNumber = request.PhoneNumber?.Trim() ?? stakeholder.PhoneNumber;

            // Check email duplicate when email is being changed
            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var trimmedEmail = request.Email.Trim();
                if (!string.Equals(stakeholder.Email, trimmedEmail, StringComparison.OrdinalIgnoreCase))
                {
                    var lowerEmail = trimmedEmail.ToLower();
                    var emailExists = await _unitOfWork.Repository<Stakeholder>()
                        .Query()
                        .AnyAsync(s => s.InternshipId == stakeholder.InternshipId
                                    && s.Email.ToLower() == lowerEmail
                                    && s.Id != request.StakeholderId, cancellationToken);

                    if (emailExists)
                    {
                        _logger.LogWarning("Stakeholder email {Email} already exists in internship {InternshipId}", request.Email, stakeholder.InternshipId);
                        return Result<UpdateStakeholderResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Stakeholder.EmailExists),
                            ResultErrorType.Conflict);
                    }
                }
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Domain encapsulation
                stakeholder.UpdateDetails(
                    name,
                    type,
                    email,
                    role,
                    description,
                    phoneNumber
                );

                await _unitOfWork.Repository<Stakeholder>().UpdateAsync(stakeholder, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(StakeholderCacheKeys.StakeholderListPattern(stakeholder.InternshipId), cancellationToken);
                await _cacheService.RemoveAsync(StakeholderCacheKeys.Stakeholder(request.StakeholderId), cancellationToken);

                _logger.LogInformation("Successfully updated stakeholder {Id}", request.StakeholderId);

                var response = _mapper.Map<UpdateStakeholderResponse>(stakeholder);
                return Result<UpdateStakeholderResponse>.Success(
                    response,
                    _messageService.GetMessage(MessageKeys.Stakeholder.UpdateSuccess)
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while updating stakeholder {Id}", request.StakeholderId);
                return Result<UpdateStakeholderResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}
