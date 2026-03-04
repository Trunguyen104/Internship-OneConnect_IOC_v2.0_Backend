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

        public DeleteStakeholderHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMessageService messageService,
            ILogger<DeleteStakeholderHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<DeleteStakeholderResponse>> Handle(DeleteStakeholderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting stakeholder {Id}", request.Id);

            // Find stakeholder
            var stakeholder = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (stakeholder == null)
            {
                _logger.LogWarning("Stakeholder {Id} not found", request.Id);
                return Result<DeleteStakeholderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.NotFound));
            }

            // TODO: Ownership check

            try
            {
                // Soft delete
                await _unitOfWork.Repository<Stakeholder>().DeleteAsync(stakeholder, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted stakeholder {Id}", request.Id);

                var response = _mapper.Map<DeleteStakeholderResponse>(stakeholder);
                return Result<DeleteStakeholderResponse>.Success(
                    response,
                    _messageService.GetMessage(MessageKeys.Stakeholder.DeleteSuccess)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting stakeholder {Id}", request.Id);
                throw;
            }
        }
    }
}
