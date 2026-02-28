using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Application.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetProjectRescourceById
{
    public class GetProjectResourceByIdHandler : IRequestHandler<GetProjectResourceByIdQuery, Result<GetProjectResourceByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetProjectResourceByIdHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        public GetProjectResourceByIdHandler(IMapper mapper , IUnitOfWork unitOfWork, ILogger<GetProjectResourceByIdHandler> logger, IMessageService messageService) {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _messageService = messageService;
        }
        public async Task<Result<GetProjectResourceByIdResponse>> Handle(GetProjectResourceByIdQuery request, CancellationToken cancellationToken)
        {
            try {
                // Check if the project resource exists
                var resource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>().GetByIdAsync(request.ProjectResourceId, cancellationToken);
                if (resource == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ProjectResourceId);
                    return Result<GetProjectResourceByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound),
                        ResultErrorType.NotFound);
                }
               
                var response = _mapper.Map<GetProjectResourceByIdResponse>(resource);
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetByIdSuccess), request.ProjectResourceId);
                return Result<GetProjectResourceByIdResponse>.Success(response);

            } catch (Exception ex) {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetByIdError), request.ProjectResourceId);
                throw;
            }
        }
    }
}
