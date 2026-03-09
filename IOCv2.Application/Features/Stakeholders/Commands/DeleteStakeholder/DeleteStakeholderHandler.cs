﻿using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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

        public DeleteStakeholderHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMessageService messageService,
            ILogger<DeleteStakeholderHandler> logger,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
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

            // Security: Ownership check (FFA-SEC)
            var currentUserIdStr = _currentUserService.UserId;
            if (string.IsNullOrEmpty(currentUserIdStr) || !Guid.TryParse(currentUserIdStr, out var currentUserId))
            {
                return Result<DeleteStakeholderResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
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
                    _logger.LogWarning("User {UserId} attempted to delete stakeholder in internship {InternshipId} without permission", currentUserId, request.InternshipId);
                    return Result<DeleteStakeholderResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
                }
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Soft delete
                await _unitOfWork.Repository<Stakeholder>().DeleteAsync(stakeholder, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

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
