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
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Check if an enterprise with the same tax code already exists
                var existingEnterprise = await _unitOfWork.Repository<Domain.Entities.Enterprise>()
                    .ExistsAsync(e => e.TaxCode == request.TaxCode, cancellationToken);
                if (existingEnterprise)
                {
                    _logger.LogError(_messageService.GetMessage(MessageKeys.Enterprise.LogEnterpriseWithSameTaxCodeExists), request.TaxCode);
                    return Result<CreateEnterpriseResponse>.Failure(_messageService.GetMessage(_messageService.GetMessage(MessageKeys.Enterprise.EnterpriseWithSameTaxCodeExists)), ResultErrorType.Conflict);
                }
                var enterprise = new Domain.Entities.Enterprise
                {
                    EnterpriseId = Guid.NewGuid(),
                    TaxCode = request.TaxCode,
                    Name = request.Name,
                    Industry = request.Industry,
                    Description = request.Description,
                    Address = request.Address,
                    Website = request.Website,
                    IsVerified = request.IsVerified,
                    Status = (short)EnterpriseStatus.Active
                };
                var response = _mapper.Map<CreateEnterpriseResponse>(enterprise);
                await _unitOfWork.Repository<Domain.Entities.Enterprise>().AddAsync(enterprise);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                await _cacheService.RemoveByPatternAsync(EnterpriseCacheKeys.EnterpriseListPattern(), cancellationToken);
                return Result<CreateEnterpriseResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Enterprise.ErrorCreatingEnterprise));
                return Result<CreateEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.ErrorCreatingEnterprise),ResultErrorType.InternalServerError);
            }
        }
    }
}
