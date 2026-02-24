﻿﻿using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Stakeholders.Commands.CreateStakeholder
{
    public class CreateStakeholderHandler : IRequestHandler<CreateStakeholderCommand, Result<CreateStakeholderResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public CreateStakeholderHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<CreateStakeholderResponse>> Handle(CreateStakeholderCommand request, CancellationToken cancellationToken)
        {
            // Check project exists
            var projectExists = await _unitOfWork.Repository<Project>()
                .ExistsAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (!projectExists)
                return Result<CreateStakeholderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.ProjectNotFound));

            // Parse Type string to enum
            if (!Enum.TryParse<Domain.Enums.StakeholderType>(request.Type, true, out var stakeholderType))
            {
                return Result<CreateStakeholderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Stakeholder.InvalidType),
                    ResultErrorType.BadRequest);
            }

            // Check email duplicate within same project (case-insensitive)
            var trimmedEmail = request.Email.Trim().ToLower();
            var emailExists = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .AnyAsync(s => s.ProjectId == request.ProjectId
                            && s.Email.ToLower() == trimmedEmail, cancellationToken);

            if (emailExists)
                return Result<CreateStakeholderResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Stakeholder.EmailExists),
                    ResultErrorType.Conflict);

            // Create entity
            var stakeholder = new Stakeholder
            {
                Id          = Guid.NewGuid(),
                ProjectId   = request.ProjectId,
                Name        = request.Name.Trim(),
                Type        = stakeholderType,
                Role        = string.IsNullOrWhiteSpace(request.Role) ? null : request.Role.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                Email       = request.Email.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim()
            };

            // Persist
            await _unitOfWork.Repository<Stakeholder>().AddAsync(stakeholder, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            // Return response
            var response = _mapper.Map<CreateStakeholderResponse>(stakeholder);
            return Result<CreateStakeholderResponse>.Success(
                response, 
                _messageService.GetMessage(MessageKeys.Stakeholder.CreateSuccess)
            );
        }
    }
}
