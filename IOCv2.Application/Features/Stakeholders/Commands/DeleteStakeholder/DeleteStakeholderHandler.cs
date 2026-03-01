﻿using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Stakeholders.Commands.DeleteStakeholder
{
    public class DeleteStakeholderHandler : IRequestHandler<DeleteStakeholderCommand, Result<DeleteStakeholderResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public DeleteStakeholderHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<DeleteStakeholderResponse>> Handle(DeleteStakeholderCommand request, CancellationToken cancellationToken)
        {
            // Find stakeholder
            var stakeholder = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

            if (stakeholder == null)
            {
                return Result<DeleteStakeholderResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.NotFound));
            }

            // Soft delete
            await _unitOfWork.Repository<Stakeholder>().DeleteAsync(stakeholder, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            var response = _mapper.Map<DeleteStakeholderResponse>(stakeholder);
            return Result<DeleteStakeholderResponse>.Success(
                response,
                _messageService.GetMessage(MessageKeys.Stakeholder.DeleteSuccess)
            );
        }
    }
}
