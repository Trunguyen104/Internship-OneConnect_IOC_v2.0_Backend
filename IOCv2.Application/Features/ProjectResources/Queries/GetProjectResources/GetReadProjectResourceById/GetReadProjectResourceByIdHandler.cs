using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ProjectResources;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetReadProjectResourceById
{
    /// <summary>
    /// Handler to retrieve a single ProjectResource by id for read operations.
    /// Combines stored relative URL with storage domain to produce a complete ResourceUrl.
    /// </summary>
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

        /// <summary>
        /// Retrieves a ProjectResource by id, maps to DTO, and returns full URL for client consumption.
        /// Returns NotFound when resource doesn't exist and InternalServerError on unexpected exceptions.
        /// </summary>
        public Task<Result<GetReadProjectResourceByIdResponse>> Handle(GetReadProjectResourceByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Query repository for the resource (read-only).
                var resource = _unitOfWork.Repository<Domain.Entities.ProjectResources>().Query().FirstOrDefault(x => x.ProjectResourceId == request.ResourceId);
                if (resource == null)
                {
                    // Resource not found — log warning and return NotFound result.
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ResourceId);
                    return Task.FromResult(Result<GetReadProjectResourceByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound),
                        ResultErrorType.NotFound));
                }

                // Combine storage domain with stored relative path so clients receive a valid download URL.
                resource.ResourceUrl = ProjectResourceParams.FileParams.FileDomain + resource.ResourceUrl;

                // Map domain entity to response DTO and return success.
                var response = _mapper.Map<GetReadProjectResourceByIdResponse>(resource);
                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetByIdSuccess), request.ResourceId);
                return Task.FromResult(Result<GetReadProjectResourceByIdResponse>.Success(response));
            }
            catch (Exception ex)
            {
                // Log unexpected exceptions and return a generic internal error.
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetByIdError), request.ResourceId);
                return Task.FromResult(Result<GetReadProjectResourceByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError));
            }
        }
    }
}