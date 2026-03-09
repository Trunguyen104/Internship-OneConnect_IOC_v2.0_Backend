using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetReadProjectResourceById
{
    public class GetReadProjectResourceByIdHandler : IRequestHandler<GetReadProjectResourceByIdQuery, Result<GetReadProjectResourceByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetReadProjectResourceByIdHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly IFileStorageService _fileStorageService;
        public GetReadProjectResourceByIdHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetReadProjectResourceByIdHandler> logger, IMessageService messageService, IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _messageService = messageService;
            _fileStorageService = fileStorageService;
        }
        public Task<Result<GetReadProjectResourceByIdResponse>> Handle(GetReadProjectResourceByIdQuery request, CancellationToken cancellationToken)
        {
            try {
                var resource = _unitOfWork.Repository<Domain.Entities.ProjectResources>().Query().FirstOrDefault(x => x.ProjectResourceId == request.ResourceId);
                if (resource == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ResourceId);
                    return Task.FromResult(Result<GetReadProjectResourceByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound),
                        ResultErrorType.NotFound));
                }
                resource.ResourceUrl = _fileStorageService.GetDomainUrl() + resource.ResourceUrl;
                var response = _mapper.Map<GetReadProjectResourceByIdResponse>(resource);
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetByIdSuccess), request.ResourceId);
                return Task.FromResult(Result<GetReadProjectResourceByIdResponse>.Success(response));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetByIdError), request.ResourceId);
                return Task.FromResult(Result<GetReadProjectResourceByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError));
            }
        }
    }
}
