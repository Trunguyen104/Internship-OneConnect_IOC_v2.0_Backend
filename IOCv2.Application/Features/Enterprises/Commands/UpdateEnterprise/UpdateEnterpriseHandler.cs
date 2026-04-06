using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Authentication.Commands.Login;
using IOCv2.Application.Features.Enterprises.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Services;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        private readonly ICacheService _cacheService;

        public UpdateEnterpriseHandler(IUnitOfWork unitOfWork, IMessageService messageService, ILogger<UpdateEnterpriseHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IRateLimiter rateLimiter, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _rateLimiter = rateLimiter;
            _cacheService = cacheService;
        }

        public async Task<Result<UpdateEnterpriseResponse>> Handle(UpdateEnterpriseCommand request, CancellationToken cancellationToken)
        {
            // 1. Pre-validation checks (Rate limit, existence, permissions)
            var rateLimitKey = _messageService.GetMessage(MessageKeys.Enterprise.RateLimitUpdateAttempt, _currentUserService.UserId!);
            if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken))
            {
                return Result<UpdateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.RequestManyTimes), ResultErrorType.TooManyRequests);
            }

            var enterprise = await _unitOfWork.Repository<Enterprise>().GetByIdAsync(request.EnterpriseId, cancellationToken);
            if (enterprise == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Enterprise.LogNotFound), request.EnterpriseId);
                return Result<UpdateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.NotFound), ResultErrorType.NotFound);
            }

            var previousEnterpriseStatus = enterprise.Status;
            var isSuperAdmin = _currentUserService.Role != null &&
                               _currentUserService.Role.Equals(UserRole.SuperAdmin.ToString(), StringComparison.OrdinalIgnoreCase);

            if (!_currentUserService.Role!.Equals(UserRole.SuperAdmin.ToString()))
            {
                bool canUpdate = await _unitOfWork.Repository<EnterpriseUser>().ExistsAsync(x => x.UserId == Guid.Parse(_currentUserService.UserId!) && x.EnterpriseId == request.EnterpriseId, cancellationToken);
                if (!canUpdate)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.Enterprise.LogUpdatePermissionsNotAllowed));
                    return Result<UpdateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.UpdatePermissionsNotAllowed), ResultErrorType.Forbidden);
                }
            }

            // Check for duplicate tax code for OTHER enterprises
            var duplicateTaxCode = await _unitOfWork.Repository<Enterprise>()
                .ExistsAsync(x => x.TaxCode == request.TaxCode && x.EnterpriseId != request.EnterpriseId, cancellationToken);
            if (duplicateTaxCode)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Enterprise.LogEnterpriseWithSameTaxCodeExists), request.TaxCode);
                return Result<UpdateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.EnterpriseWithSameTaxCodeExists), ResultErrorType.Conflict);
            }

            // 2. Begin Transaction (Only for writing)
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                _mapper.Map(request, enterprise);

                // BR-ENT-TG-01: Cascading suspend/block enterprise (Removed - Suspended status not used)


                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // 3. Post-commit operations
                await _cacheService.RemoveByPatternAsync(EnterpriseCacheKeys.EnterpriseListPattern(), cancellationToken);

                return Result<UpdateEnterpriseResponse>.Success(_mapper.Map<UpdateEnterpriseResponse>(enterprise));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                // Track failed attempt on DB error
                await _rateLimiter.RegisterFailAsync(rateLimitKey, limit: 5, window: TimeSpan.FromMinutes(1), blockFor: TimeSpan.FromMinutes(1), cancellationToken);

                if (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Result<UpdateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.EnterpriseWithSameTaxCodeExists), ResultErrorType.Conflict);
                }

                throw; // Let GlobalExceptionHandler handle unexpected errors
            }
        }
    }
}