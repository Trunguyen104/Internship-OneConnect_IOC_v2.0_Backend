﻿﻿using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Stakeholders.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder
{
    public class CreateStakeholderHandler : IRequestHandler<CreateStakeholderCommand, Result<CreateStakeholderResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateStakeholderHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public CreateStakeholderHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<CreateStakeholderHandler> logger,
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

        public async Task<Result<CreateStakeholderResponse>> Handle(CreateStakeholderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating stakeholder {Name} for internship {InternshipId}", request.Name, request.InternshipId);

            // Check internship group exists
            var internshipExists = await _unitOfWork.Repository<InternshipGroup>()
                .ExistsAsync(p => p.InternshipId == request.InternshipId, cancellationToken);

            if (!internshipExists)
            {
                _logger.LogWarning("InternshipGroup {InternshipId} not found", request.InternshipId);
                return Result<CreateStakeholderResponse>.NotFound("InternshipGroup not found");
            }

            var authError = StakeholderAccessGuard.EnsureAuthenticated<CreateStakeholderResponse>(_currentUserService, _messageService);
            if (authError is not null)
            {
                return authError;
            }

            var managePermissionError = StakeholderAccessGuard.EnsureManagePermission<CreateStakeholderResponse>(_currentUserService, _messageService);
            if (managePermissionError is not null)
            {
                _logger.LogWarning("User {UserId} with role {Role} attempted to create stakeholder in internship {InternshipId}", _currentUserService.UserId, _currentUserService.Role, request.InternshipId);
                return managePermissionError;
            }

            var accessError = await StakeholderAccessGuard.EnsureInternshipAccessAsync<CreateStakeholderResponse>(
                _unitOfWork,
                _messageService,
                _currentUserService,
                request.InternshipId,
                cancellationToken);

            if (accessError is not null)
            {
                return accessError;
            }

            // Check email duplicate within same internship
            var trimmedEmail = request.Email.Trim().ToLower();
            var emailExists = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .AnyAsync(s => s.InternshipId == request.InternshipId
                            && s.Email.ToLower() == trimmedEmail, cancellationToken);

            if (emailExists)
            {
                _logger.LogWarning("Stakeholder email {Email} already exists in internship {InternshipId}", request.Email, request.InternshipId);
                return Result<CreateStakeholderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Stakeholder.EmailExists),
                    ResultErrorType.Conflict);
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Create entity using Rich Domain Model constructor
                var stakeholder = new Stakeholder(
                    request.InternshipId,
                    request.Name.Trim(),
                    request.Type,
                    request.Email.Trim(),
                    request.Role?.Trim(),
                    request.Description?.Trim(),
                    request.PhoneNumber?.Trim()
                );

                await _unitOfWork.Repository<Stakeholder>().AddAsync(stakeholder, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(StakeholderCacheKeys.StakeholderListPattern(request.InternshipId), cancellationToken);

                _logger.LogInformation("Successfully created stakeholder {StakeholderId} for internship {InternshipId}",
                    stakeholder.Id, request.InternshipId);

                var response = _mapper.Map<CreateStakeholderResponse>(stakeholder);
                return Result<CreateStakeholderResponse>.Success(
                    response, 
                    _messageService.GetMessage(MessageKeys.Stakeholder.CreateSuccess)
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while creating stakeholder for internship {InternshipId}", request.InternshipId);
                return Result<CreateStakeholderResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}
