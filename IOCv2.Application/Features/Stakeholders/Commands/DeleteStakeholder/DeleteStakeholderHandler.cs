﻿using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Stakeholders.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder
{
    public class DeleteStakeholderHandler : IRequestHandler<DeleteStakeholderCommand, Result<DeleteStakeholderResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<DeleteStakeholderHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public DeleteStakeholderHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<DeleteStakeholderHandler> logger,
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

        public async Task<Result<DeleteStakeholderResponse>> Handle(DeleteStakeholderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting stakeholder {Id}", request.StakeholderId);

            // Find stakeholder
            var stakeholder = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .FirstOrDefaultAsync(s => s.Id == request.StakeholderId && s.InternshipId == request.InternshipId, cancellationToken);

            if (stakeholder == null)
            {
                _logger.LogWarning("Stakeholder {Id} not found in internship {InternshipId}", request.StakeholderId, request.InternshipId);
                return Result<DeleteStakeholderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.NotFound));
            }

            var authError = StakeholderAccessGuard.EnsureAuthenticated<DeleteStakeholderResponse>(_currentUserService, _messageService);
            if (authError is not null)
            {
                return authError;
            }

            var managePermissionError = StakeholderAccessGuard.EnsureManagePermission<DeleteStakeholderResponse>(_currentUserService, _messageService);
            if (managePermissionError is not null)
            {
                _logger.LogWarning("User {UserId} with role {Role} attempted to delete stakeholder {StakeholderId}", _currentUserService.UserId, _currentUserService.Role, request.StakeholderId);
                return managePermissionError;
            }

            var accessError = await StakeholderAccessGuard.EnsureInternshipAccessAsync<DeleteStakeholderResponse>(
                _unitOfWork,
                _messageService,
                _currentUserService,
                request.InternshipId,
                cancellationToken);

            if (accessError is not null)
            {
                _logger.LogWarning("User {UserId} attempted to delete stakeholder in internship {InternshipId} without permission", _currentUserService.UserId, request.InternshipId);
                return accessError;
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Soft delete
                await _unitOfWork.Repository<Stakeholder>().DeleteAsync(stakeholder, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(StakeholderCacheKeys.StakeholderListPattern(stakeholder.InternshipId), cancellationToken);
                await _cacheService.RemoveAsync(StakeholderCacheKeys.Stakeholder(request.StakeholderId), cancellationToken);

                _logger.LogInformation("Successfully deleted stakeholder {Id}", request.StakeholderId);

                var response = _mapper.Map<DeleteStakeholderResponse>(stakeholder);
                return Result<DeleteStakeholderResponse>.Success(
                    response,
                    _messageService.GetMessage(MessageKeys.Stakeholder.DeleteSuccess)
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while deleting stakeholder {Id}", request.StakeholderId);
                return Result<DeleteStakeholderResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}
