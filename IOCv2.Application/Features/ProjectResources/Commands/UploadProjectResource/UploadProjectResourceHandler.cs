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
                // Validate file name and file size before processing upload
                var fileValidation = FileValidationHelper.ValidateFile(
                    request.File.FileName,
                    request.File.Length);

                if (!fileValidation.IsValid)
                {
                    // Return BadRequest if file validation fails
                    return Result<UploadProjectResourceResponse>.Failure(
                        fileValidation.ErrorMessage!,
                        ResultErrorType.BadRequest);
                }

                // Check if the target project exists in the database
                var projectExists = await _unitOfWork.Repository<Project>()
                    .ExistsAsync(p => p.ProjectId == request.ProjectId, cancellationToken);
                if (!projectExists)
                {
                    // Return NotFound if project does not exist
                    return Result<UploadProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.NotFound),
                        ResultErrorType.NotFound);
                }
                // Start database transaction
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                string? fileUrl = null;
                try
                {
                    // Generate a unique file name to avoid collisions
                    var fileName = FileParams.GetFileName(request.File.FileName);
                    // Upload file to storage service (local, S3, etc.)
                    fileUrl = await _fileStorageService.UploadFileAsync(
                        request.File,
                        FileParams.GetFolder(request.ProjectId),
                        fileName,
                        cancellationToken);
                    FileType fileType;
                    try
                    {
                        // Automatically detect file type based on file extension
                        fileType = AutoSetFileType(fileUrl);
                    }
                    catch (Exception ex)
                    {
                        // Log error if file type detection fails
                        _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUploadAutoSetFileTypeError), fileUrl);
                        // Reject unsupported file types
                        return Result<UploadProjectResourceResponse>.Failure(_messageService.GetMessage("MessageKeys.ProjectResourcesKey.UnsupportedFileType"), ResultErrorType.BadRequest);
                    }
                    // Create ProjectResources entity
                    var resource = new Domain.Entities.ProjectResources(
                        request.ProjectId,
                        request.ResourceName ?? request.File.FileName,
                        fileType,
                        fileUrl);

                    // Save resource metadata to database
                    await _unitOfWork.Repository<Domain.Entities.ProjectResources>().AddAsync(resource, cancellationToken);
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                    // Commit transaction after successful database operation
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    // Map entity to response DTO
                    var response = _mapper.Map<UploadProjectResourceResponse>(resource);
                    // Log successful upload
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUploadSuccess),
                        request.File.FileName, request.ProjectId);
                    return Result<UploadProjectResourceResponse>.Success(response);
                }
                catch (Exception ex)
                {
                    // Rollback transaction if any error occurs
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    // Compensating action:
                    // If the file was uploaded but database operation failed,
                    // delete the uploaded file to prevent orphaned files in storage
                    if (!string.IsNullOrEmpty(fileUrl))
                    {
                        await _fileStorageService.DeleteFileAsync(fileUrl, cancellationToken);
                    }
                    // Log upload failure
                    _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUploadError), request.ProjectId);
                    return Result<UploadProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
                }
            }
            catch (Exception ex)
            {
                // Catch unexpected errors during validation or initialization
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUploadError), request.ProjectId);
                return Result<UploadProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }

        private FileType AutoSetFileType(string filePath)
        {
            var extension = System.IO.Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                FileParams.PdfExtension => FileType.PDF,
                FileParams.DocxExtension => FileType.DOCX,
                FileParams.PptxExtension => FileType.PPTX,
                FileParams.ZipExtension => FileType.ZIP,
                FileParams.RarExtension => FileType.RAR,
                FileParams.JpgExtension => FileType.JPG,
                FileParams.JpegExtension => FileType.JPG,
                FileParams.PngExtension => FileType.PNG,
                _ => throw new InvalidOperationException(_messageService.GetMessage("MessageKeys.ProjectResourcesKey.UnsupportedFileType"))
            };
        }
    }
}