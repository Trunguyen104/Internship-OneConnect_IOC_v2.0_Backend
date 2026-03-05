using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
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
        public DeleteEnterpriseHandler(IUnitOfWork unitOfWork, IMessageService messageService, ILogger<DeleteEnterpriseHandler> logger, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
        }
        public async Task<Result<DeleteEnterpriseResponse>> Handle(DeleteEnterpriseCommand request, CancellationToken cancellationToken)
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var enterprise = await _unitOfWork.Repository<Enterprise>().GetByIdAsync(request.EnterpriseId, cancellationToken);
                if (enterprise == null)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.Enterprise.LogNotFound), request.EnterpriseId);
                    return Result<DeleteEnterpriseResponse>.NotFound(_messageService.GetMessage(MessageKeys.Enterprise.NotFound));
                }
                enterprise.DeletedAt = DateTime.UtcNow;
                var response = _mapper.Map<DeleteEnterpriseResponse>(enterprise);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return Result<DeleteEnterpriseResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Enterprise.LogDeleteError), request.EnterpriseId);
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                return Result<DeleteEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.DeleteError), ResultErrorType.InternalServerError);
            }
        }
    }
}
