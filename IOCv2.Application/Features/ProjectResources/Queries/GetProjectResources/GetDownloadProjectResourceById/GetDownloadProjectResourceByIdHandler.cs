using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ProjectResources;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetDownloadProjectResourceById
{
    public class GetDownloadProjectResourceByIdHandler : IRequestHandler<GetDownloadProjectResourceByIdQuery, Result<GetDownloadProjectResourceByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GetDownloadProjectResourceByIdHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly IFileStorageService _fileStorageService;
        public GetDownloadProjectResourceByIdHandler(IMapper mapper, IUnitOfWork unitOfWork, ILogger<GetDownloadProjectResourceByIdHandler> logger, IMessageService messageService, IFileStorageService fileStorageService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _messageService = messageService;
            _fileStorageService = fileStorageService;
        }

        /// <summary>
        /// Handles the GetDownloadProjectResourceByIdQuery to retrieve a project resource by its ID and return a file stream for download.
        /// </summary>
        /// <param name="request">The request containing the project resource ID.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Result containing the response with the file stream for download.</returns>
        public async Task<Result<GetDownloadProjectResourceByIdResponse>> Handle(GetDownloadProjectResourceByIdQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if the project resource exists
                var resource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ProjectResourceId == request.ProjectResourceId, cancellationToken);
                if (resource == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ProjectResourceId);
                    return Result<GetDownloadProjectResourceByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound),
                        ResultErrorType.NotFound);
                }

                var extension = Path.GetExtension(resource.ResourceUrl);
                Stream? stream = null;
                try
                {
                    stream = await _fileStorageService.GetFileAsync(resource.ResourceUrl, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Use localized message key instead of hard-coded string
                    _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.ErrorRetrievingFileFromStorage, request.ProjectResourceId));
                    return Result<GetDownloadProjectResourceByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound),
                        ResultErrorType.NotFound);
                }

                if (stream == null)
                {
                    return Result<GetDownloadProjectResourceByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound),
                        ResultErrorType.NotFound);
                }

                FileStreamResult fileResult;
                try
                {
                    fileResult = new FileStreamResult(stream, ProjectResourceParams.FileParams.DefaultMime)
                    {
                        FileDownloadName = string.Format(ProjectResourceParams.FileParams.GetFileDownloadName, resource.ResourceName!, extension)
                    };
                    var sad = fileResult.FileDownloadName;
                }
                catch (Exception ex)
                {
                    stream.Dispose();
                    // Use localized message key instead of hard-coded string
                    _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.ErrorCreatingFileStreamResult, request.ProjectResourceId));
                    return Result<GetDownloadProjectResourceByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.InternalError),
                        ResultErrorType.InternalServerError);
                }

                var response = new GetDownloadProjectResourceByIdResponse
                {
                    FileResponse = fileResult
                };
                return Result<GetDownloadProjectResourceByIdResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetByIdError), request.ProjectResourceId);
                return Result<GetDownloadProjectResourceByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}