﻿using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Stakeholders.Commands.UpdateStakeholder
{
    public class UpdateStakeholderHandler : IRequestHandler<UpdateStakeholderCommand, Result<UpdateStakeholderResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public UpdateStakeholderHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<UpdateStakeholderResponse>> Handle(UpdateStakeholderCommand request, CancellationToken cancellationToken)
        {
            // Find stakeholder
            var stakeholder = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (stakeholder == null)
            {
                return Result<UpdateStakeholderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.NotFound));
            }

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
                        return Result<UpdateStakeholderResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Stakeholder.EmailExists),
                            ResultErrorType.Conflict);
                    }
                }
            }

            // Partial update
            if (!string.IsNullOrWhiteSpace(request.Name))
                stakeholder.Name = request.Name.Trim();

            if (request.Type.HasValue)
                stakeholder.Type = request.Type.Value;

            if (request.Role != null)
                stakeholder.Role = string.IsNullOrWhiteSpace(request.Role) ? null : request.Role.Trim();

            if (request.Description != null)
                stakeholder.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();

            if (!string.IsNullOrWhiteSpace(request.Email))
                stakeholder.Email = request.Email.Trim();

            if (request.PhoneNumber != null)
                stakeholder.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim();

            // Persist
            await _unitOfWork.Repository<Stakeholder>().UpdateAsync(stakeholder, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Return response
            var response = _mapper.Map<UpdateStakeholderResponse>(stakeholder);
            return Result<UpdateStakeholderResponse>.Success(
                response,
                _messageService.GetMessage(MessageKeys.Stakeholder.UpdateSuccess)
            );
        }
    }
}
