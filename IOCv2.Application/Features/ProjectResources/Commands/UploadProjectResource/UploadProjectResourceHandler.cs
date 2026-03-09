using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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

namespace IOCv2.Application.Features.ProjectResources.Commands.UploadProjectResource
{

    public class UploadProjectResourceHandler : IRequestHandler<UploadProjectResourceCommand, Result<UploadProjectResourceResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UploadProjectResourceHandler> _logger;
        private readonly IFileStorageService _fileStorageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;

        public UploadProjectResourceHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UploadProjectResourceHandler> logger,
            IFileStorageService fileStorageService,
            ICurrentUserService currentUserService,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _currentUserService = currentUserService;
            _messageService = messageService;
        }

        public async Task<Result<UploadProjectResourceResponse>> Handle(
            UploadProjectResourceCommand request,
            CancellationToken cancellationToken)
        {
            try
            {

                // Validate file
                var fileValidation = FileValidationHelper.ValidateFile(
                    request.File.FileName,
                    request.File.Length);

                if (!fileValidation.IsValid)
                {
                    return Result<UploadProjectResourceResponse>.Failure(
                        fileValidation.ErrorMessage!,
                        ResultErrorType.BadRequest);
                }

                // Check if project exists
                var projectExists = await _unitOfWork.Repository<Project>()
                    .ExistsAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

                if (!projectExists)
                {
                    return Result<UploadProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.NotFound),
                        ResultErrorType.NotFound);
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                string? fileUrl = null;
                try
                {
                    // Upload file to storage
                    var fileName = $"{Guid.NewGuid():N}_{request.File.FileName}";
                    fileUrl = await _fileStorageService.UploadFileAsync(
                        request.File,
                        $"projects/{request.ProjectId}/resources",
                        fileName,
                        cancellationToken);

                    // Create resource record using constructor
                    // Create resource record using constructor
                    var resource = new Domain.Entities.ProjectResources(
                        request.ProjectId,
                        request.ResourceName ?? request.File.FileName,
                        request.ResourceType,
                        fileUrl);


                    // Save to database
                    await _unitOfWork.Repository<Domain.Entities.ProjectResources>().AddAsync(resource, cancellationToken);
                    await _unitOfWork.SaveChangeAsync(cancellationToken);

                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    // Map to response
                    var response = _mapper.Map<UploadProjectResourceResponse>(resource);

                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUploadSuccess),
                        request.File.FileName, request.ProjectId);

                    return Result<UploadProjectResourceResponse>.Success(response);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    
                    // Compensating transaction: remove the uploaded file if database commit fails
                    if (!string.IsNullOrEmpty(fileUrl))
                    {
                        try
                        {
                            await _fileStorageService.DeleteFileAsync(fileUrl, cancellationToken);
                            _logger.LogInformation("Compensatory action: Deleted orphaned file {FileUrl} from storage after database failure.", fileUrl);
                        }
                        catch (Exception deleteEx)
                        {
                            _logger.LogError(deleteEx, "Compensatory action failed: Could not delete orphaned file {FileUrl} from storage.", fileUrl);
                        }
                    }

                    _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUploadError), request.ProjectId);
                    return Result<UploadProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
                }
            }
            catch (Exception ex) // Outer catch for validation or project existence checks exceptions
            {
                _logger.LogError(ex, "Failed to initiate file upload process for project: {ProjectId}", request.ProjectId);
                return Result<UploadProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}
