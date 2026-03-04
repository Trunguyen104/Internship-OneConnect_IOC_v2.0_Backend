﻿using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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

        public UpdateStakeholderHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMessageService messageService,
            ILogger<UpdateStakeholderHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<UpdateStakeholderResponse>> Handle(UpdateStakeholderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating stakeholder {Id}", request.Id);

            // Find stakeholder
            var stakeholder = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (stakeholder == null)
            {
                _logger.LogWarning("Stakeholder {Id} not found", request.Id);
                return Result<UpdateStakeholderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.NotFound));
            }

            // TODO: Ownership check

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
                        .AnyAsync(s => s.ProjectId == stakeholder.ProjectId
                                    && s.Email.ToLower() == lowerEmail
                                    && s.Id != request.Id, cancellationToken);

                    if (emailExists)
                    {
                        _logger.LogWarning("Stakeholder email {Email} already exists in project {ProjectId}", request.Email, stakeholder.ProjectId);
                        return Result<UpdateStakeholderResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Stakeholder.EmailExists),
                            ResultErrorType.Conflict);
                    }
                }
            }

            try
            {
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

                _logger.LogInformation("Successfully updated stakeholder {Id}", request.Id);

                var response = _mapper.Map<UpdateStakeholderResponse>(stakeholder);
                return Result<UpdateStakeholderResponse>.Success(
                    response,
                    _messageService.GetMessage(MessageKeys.Stakeholder.UpdateSuccess)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating stakeholder {Id}", request.Id);
                throw;
            }
        }
    }
}
