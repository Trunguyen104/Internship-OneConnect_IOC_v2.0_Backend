using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Enterprises.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.CreateEnterprise
{
    public class CreateEnterpriseHandler : MediatR.IRequestHandler<CreateEnterpriseCommand, Result<CreateEnterpriseResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateEnterpriseHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICacheService _cacheService;

        public CreateEnterpriseHandler(IUnitOfWork unitOfWork, IMessageService messageService, ILogger<CreateEnterpriseHandler> logger, IMapper mapper, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
            _cacheService = cacheService;
        }

        public async Task<Result<CreateEnterpriseResponse>> Handle(CreateEnterpriseCommand request, CancellationToken cancellationToken)
        {
            // 1. Pre-validation checks (Before opening transaction)
            var existingEnterprise = await _unitOfWork.Repository<Domain.Entities.Enterprise>()
                .ExistsAsync(e => e.TaxCode == request.TaxCode, cancellationToken);
            if (existingEnterprise)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Enterprise.LogEnterpriseWithSameTaxCodeExists), request.TaxCode);
                return Result<CreateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.EnterpriseWithSameTaxCodeExists), ResultErrorType.Conflict);
            }

            // 2. Begin Transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var enterprise = new Domain.Entities.Enterprise
                {
                    EnterpriseId = Guid.NewGuid(),
                    TaxCode = request.TaxCode,
                    Name = request.Name,
                    Industry = request.Industry,
                    Description = request.Description,
                    Address = request.Address,
                    Website = request.Website,
                    Status = (short)EnterpriseStatus.Active
                };

                await _unitOfWork.Repository<Domain.Entities.Enterprise>().AddAsync(enterprise);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // 3. Post-commit operations (Cache invalidation)
                await _cacheService.RemoveByPatternAsync(EnterpriseCacheKeys.EnterpriseListPattern(), cancellationToken);

                return Result<CreateEnterpriseResponse>.Success(_mapper.Map<CreateEnterpriseResponse>(enterprise));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                
                // Specific check for duplicate constraint from DB
                if (ex.InnerException?.Message.Contains("duplicate", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Result<CreateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.EnterpriseWithSameTaxCodeExists), ResultErrorType.Conflict);
                }

                throw; // Let GlobalExceptionHandler handle unexpected errors
            }
        }
    }
}
