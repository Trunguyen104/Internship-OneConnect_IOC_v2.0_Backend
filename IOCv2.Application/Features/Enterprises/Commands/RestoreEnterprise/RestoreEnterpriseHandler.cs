using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Commands.RestoreEnterprise
{
    public class RestoreEnterpriseHandler : IRequestHandler<RestoreEnterpriseCommand, Result<RestoreEnterpriseResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<RestoreEnterpriseHandler> _logger;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;

        public RestoreEnterpriseHandler(IUnitOfWork unitOfWork, IMessageService messageService, ILogger<RestoreEnterpriseHandler> logger, IMapper mapper, ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
        }

        public async Task<Result<RestoreEnterpriseResponse>> Handle(RestoreEnterpriseCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check Enterprise Exist
                var enterprise = await _unitOfWork.Repository<Domain.Entities.Enterprise>().Query().IgnoreQueryFilters().FirstOrDefaultAsync(x => x.EnterpriseId == request.EnterpriseId, cancellationToken);
                if (enterprise == null)
                {
                    return Result<RestoreEnterpriseResponse>.NotFound(_messageService.GetMessage(MessageKeys.Enterprise.NotFound));
                }
                // Verify that user belong to the target enterprise
                if (!_currentUserService.Role!.Equals(UserRole.SuperAdmin.ToString()))
                {
                    bool canRestore = await _unitOfWork.Repository<Domain.Entities.EnterpriseUser>().ExistsAsync(x => x.UserId == Guid.Parse(_currentUserService.UserId!) && x.EnterpriseId == request.EnterpriseId, cancellationToken);
                    if (!canRestore)
                    {
                        return Result<RestoreEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.RestorePermissionDenied), ResultErrorType.Forbidden);
                    }
                }
                if (enterprise.DeletedAt == null)
                {
                    return Result<RestoreEnterpriseResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Enterprise.NotDeleted)
                    );
                }
                // Restore enterprise
                enterprise.DeletedAt = null;
                var response = _mapper.Map<RestoreEnterpriseResponse>(enterprise);
                await _unitOfWork.Repository<Domain.Entities.Enterprise>().UpdateAsync(enterprise);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                return Result<RestoreEnterpriseResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Enterprise.LogRestoreFailed), request.EnterpriseId);
                return Result<RestoreEnterpriseResponse>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.RestoreFailed), ResultErrorType.InternalServerError);
            }
        }
    }
}