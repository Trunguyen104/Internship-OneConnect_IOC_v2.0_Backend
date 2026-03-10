using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Authentication.Commands.Login;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Services;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.UpdateEnterprise
{
    public class UpdateEnterpriseHandler : IRequestHandler<UpdateEnterpriseCommand, Result<UpdateEnterpriseResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<UpdateEnterpriseHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IRateLimiter _rateLimiter;

        public UpdateEnterpriseHandler(IUnitOfWork unitOfWork, IMessageService messageService, ILogger<UpdateEnterpriseHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IRateLimiter rateLimiter)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _rateLimiter = rateLimiter;
        }

        public async Task<Result<UpdateEnterpriseResponse>> Handle(UpdateEnterpriseCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Each user has own key counting invalid turn
                var rateLimitKey = _messageService.GetMessage(MessageKeys.Enterprise.RateLimitUpdateAttempt, _currentUserService.UserId!);
                // Check if user is blocked due to too many failed attempts
                if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken))
                {
                    return Result<UpdateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.RequestManyTimes), ResultErrorType.TooManyRequests);
                }
                // Register failed attempt (block after 5 attempts in 1 mins)
                await _rateLimiter.RegisterFailAsync(
                    rateLimitKey,
                    limit: 5,
                    window: TimeSpan.FromMinutes(1),
                    blockFor: TimeSpan.FromMinutes(1),
                    cancellationToken);
                // Check Enterprise Exist
                // Retrieve enterprise by id
                var enterprise = await _unitOfWork.Repository<Enterprise>().GetByIdAsync(request.EnterpriseId, cancellationToken);
                // If enterprise not found → return 404
                if (enterprise == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.Enterprise.LogNotFound), request.EnterpriseId);
                    return Result<UpdateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.NotFound), ResultErrorType.NotFound);
                }
                // Verify that user belong to the target enterprise
                if (_currentUserService.Role != "SuperAdmin")
                {
                    bool canUpdate = await _unitOfWork.Repository<EnterpriseUser>().ExistsAsync(x => x.UserId == CurrentUserHelper.GetValidGuidUserId(_currentUserService.UserId!) && x.EnterpriseId == request.EnterpriseId, cancellationToken);
                    // Verify that user has permission to update enterprise
                    if (!canUpdate)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.Enterprise.LogUpdatePermissionsNotAllowed));
                        return Result<UpdateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.UpdatePermissionsNotAllowed), ResultErrorType.Forbidden);
                    }
                //if (enterprise.TaxCode != request.TaxCode) return Result<UpdateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.UpdateTaxCodeNotAllowed), ResultErrorType.Forbidden);
                }   

                // Map updated fields from request into existing entity
                _mapper.Map(request, enterprise);

                // Map entity to response DTO
                var response = _mapper.Map<UpdateEnterpriseResponse>(enterprise);
                // Persist changes

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<UpdateEnterpriseResponse>.Success(response);
            }
            catch (Exception ex)
            {
                // Rollback transaction on failure
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                // Log unexpected exception
                _logger.LogError(ex.Message, ResultErrorType.InternalServerError);
                throw;
            }
        }
    }
}
