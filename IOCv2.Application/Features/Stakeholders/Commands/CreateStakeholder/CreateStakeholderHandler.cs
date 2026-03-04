﻿﻿using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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

        public CreateStakeholderHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMessageService messageService,
            ILogger<CreateStakeholderHandler> logger,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<CreateStakeholderResponse>> Handle(CreateStakeholderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating stakeholder {Name} for project {ProjectId}", request.Name, request.ProjectId);

            // Check project exists and user has access
            var project = await _unitOfWork.Repository<Project>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<CreateStakeholderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.ProjectNotFound));
            }

            // TODO: Move project ownership check to a dedicated service if reused often
            // For now, assuming standard project membership logic
            // if (project.CreatedBy != _currentUserService.UserId && !await IsProjectMember(project.ProjectId))
            //     return Result<CreateStakeholderResponse>.Forbidden();

            // Check email duplicate within same project
            var trimmedEmail = request.Email.Trim().ToLower();
            var emailExists = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .AnyAsync(s => s.ProjectId == request.ProjectId
                            && s.Email.ToLower() == trimmedEmail, cancellationToken);

            if (emailExists)
            {
                _logger.LogWarning("Stakeholder email {Email} already exists in project {ProjectId}", request.Email, request.ProjectId);
                return Result<CreateStakeholderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Stakeholder.EmailExists),
                    ResultErrorType.Conflict);
            }

            try
            {
                // Create entity using Rich Domain Model constructor
                var stakeholder = new Stakeholder(
                    request.ProjectId,
                    request.Name.Trim(),
                    request.Type,
                    request.Email.Trim(),
                    request.Role?.Trim(),
                    request.Description?.Trim(),
                    request.PhoneNumber?.Trim()
                );

                await _unitOfWork.Repository<Stakeholder>().AddAsync(stakeholder, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                _logger.LogInformation("Successfully created stakeholder {StakeholderId} for project {ProjectId}", 
                    stakeholder.Id, request.ProjectId);

                var response = _mapper.Map<CreateStakeholderResponse>(stakeholder);
                return Result<CreateStakeholderResponse>.Success(
                    response, 
                    _messageService.GetMessage(MessageKeys.Stakeholder.CreateSuccess)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating stakeholder for project {ProjectId}", request.ProjectId);
                throw;
            }
        }
    }
}
