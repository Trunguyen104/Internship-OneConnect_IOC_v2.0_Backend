using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Enterprises;
using IOCv2.Application.Features.Enterprises.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.DeleteEnterprise
{
    public class DeleteEnterpriseHandler : IRequestHandler<DeleteEnterpriseCommand, Result<DeleteEnterpriseResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<DeleteEnterpriseHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        public DeleteEnterpriseHandler(IUnitOfWork unitOfWork, IMessageService messageService, ILogger<DeleteEnterpriseHandler> logger, IMapper mapper, ICurrentUserService currentUserService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
        }
        public async Task<Result<DeleteEnterpriseResponse>> Handle(DeleteEnterpriseCommand request, CancellationToken cancellationToken)
        {
            // 1. Pre-validation checks
            var enterprise = await _unitOfWork.Repository<Enterprise>().GetByIdAsync(request.EnterpriseId, cancellationToken);
            if (enterprise == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Enterprise.LogNotFound), request.EnterpriseId);
                return Result<DeleteEnterpriseResponse>.NotFound(_messageService.GetMessage(MessageKeys.Enterprise.NotFound));
            }

            if (!_currentUserService.Role!.Equals(UserRole.SuperAdmin.ToString()))
            {
                bool canDelete = await _unitOfWork.Repository<EnterpriseUser>().ExistsAsync(x => x.UserId == Guid.Parse(_currentUserService.UserId!) && x.EnterpriseId == request.EnterpriseId, cancellationToken);
                if (!canDelete)
                {
                    return Result<DeleteEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.DeletePermissionDenied), ResultErrorType.Forbidden);
                }
            }

            // 2. Begin Transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                enterprise.DeletedAt = DateTime.UtcNow;
                var response = _mapper.Map<DeleteEnterpriseResponse>(enterprise);
                
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // 3. Post-commit operations
                await _cacheService.RemoveByPatternAsync(EnterpriseCacheKeys.EnterpriseListPattern(), cancellationToken);

                return Result<DeleteEnterpriseResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Enterprise.LogDeleteError), request.EnterpriseId);
                throw; // Bubble up to GlobalExceptionHandler
            }
        }
    }
}