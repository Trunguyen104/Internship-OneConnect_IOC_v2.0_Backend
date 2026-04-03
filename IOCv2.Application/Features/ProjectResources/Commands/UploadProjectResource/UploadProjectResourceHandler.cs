using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ProjectResources;
using IOCv2.Application.Features.ProjectResources.Common;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
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
        private readonly ICacheService _cacheService;

        public UploadProjectResourceHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<UploadProjectResourceHandler> logger,
            IFileStorageService fileStorageService,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _fileStorageService = fileStorageService;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<UploadProjectResourceResponse>> Handle(
            UploadProjectResourceCommand request,
            CancellationToken cancellationToken)
        {
            try
            {
                var isLinkUpload = !string.IsNullOrWhiteSpace(request.ExternalUrl);

                // Validate file name and file size before processing upload
                FileValidationResult? fileValidation = null;
                if (!isLinkUpload)
                {
                    if (request.File == null)
                    {
                        return Result<UploadProjectResourceResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.ProjectResourcesKey.FileOrLinkRequired),
                            ResultErrorType.BadRequest);
                    }

                    fileValidation = FileValidationHelper.ValidateFile(
                        request.File.FileName,
                        request.File.Length);

                    if (!fileValidation.IsValid)
                    {
                        // Return BadRequest if file validation fails
                        return Result<UploadProjectResourceResponse>.Failure(
                            fileValidation.ErrorMessage!,
                            ResultErrorType.BadRequest);
                    }
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

                var hasAccess = await HasProjectAccessAsync(request.ProjectId, cancellationToken);
                if (!hasAccess)
                {
                    return Result<UploadProjectResourceResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }

                var mentorUploaderId = await ResolveMentorUploaderIdAsync(cancellationToken);

                // Start database transaction
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                string? fileUrl = null;
                try
                {
                    FileType fileType;
                    if (isLinkUpload)
                    {
                        fileUrl = request.ExternalUrl!.Trim();
                        fileType = FileType.LINK;
                    }
                    else
                    {
                        // Generate a unique file name to avoid collisions
                        var fileName = FileParams.GetFileName(request.File!.FileName);
                        // Upload file to storage service (local, S3, etc.)
                        fileUrl = await _fileStorageService.UploadFileAsync(
                            request.File,
                            FileParams.GetFolder(request.ProjectId),
                            fileName,
                            cancellationToken);
                        // Keep the original validated file type from the incoming file name.
                        if (fileValidation?.FileType == null)
                        {
                            _logger.LogWarning("Unsupported file type for uploaded file: {FileName}", request.File.FileName);
                            return Result<UploadProjectResourceResponse>.Failure(
                                _messageService.GetMessage(MessageKeys.ProjectResourcesKey.InvalidFileType),
                                ResultErrorType.BadRequest);
                        }

                        fileType = fileValidation.FileType.Value;
                    }

                    // Create ProjectResources entity
                    var resource = new Domain.Entities.ProjectResources(
                        request.ProjectId,
                        request.ResourceName ?? request.File?.FileName ?? fileUrl,
                        fileType,
                        fileUrl);

                    if (mentorUploaderId.HasValue)
                    {
                        resource.UploadedBy = mentorUploaderId.Value;
                    }

                    // Save resource metadata to database
                    await _unitOfWork.Repository<Domain.Entities.ProjectResources>().AddAsync(resource, cancellationToken);
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                    // Commit transaction after successful database operation
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    // Map entity to response DTO
                    var response = _mapper.Map<UploadProjectResourceResponse>(resource);
                    if (response.ResourceType != FileType.LINK && !string.IsNullOrWhiteSpace(response.ResourceUrl))
                    {
                        response.ResourceUrl = _fileStorageService.GetFileUrl(response.ResourceUrl);
                    }
                    // Log successful upload
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUploadSuccess),
                        request.File?.FileName ?? request.ExternalUrl, request.ProjectId);

                    await _cacheService.RemoveByPatternAsync(ProjectResourceCacheKeys.ListPattern(request.ProjectId), cancellationToken);
                    await _cacheService.RemoveByPatternAsync(ProjectResourceCacheKeys.ListPattern(null), cancellationToken);
                    await _cacheService.RemoveAsync(ProjectCacheKeys.Project(request.ProjectId), cancellationToken);
                    await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

                    return Result<UploadProjectResourceResponse>.Success(response);
                }
                catch (Exception ex)
                {
                    // Rollback transaction if any error occurs
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    // Compensating action:
                    // If the file was uploaded but database operation failed,
                    // delete the uploaded file to prevent orphaned files in storage
                    if (!string.IsNullOrEmpty(fileUrl) && !isLinkUpload)
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

        private async Task<bool> HasProjectAccessAsync(Guid projectId, CancellationToken cancellationToken)
        {
            if (string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentUserService.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return false;
            }

            if (string.Equals(_currentUserService.Role, "Mentor", StringComparison.OrdinalIgnoreCase))
            {
                var enterpriseUserId = await _unitOfWork.Repository<EnterpriseUser>().Query()
                    .AsNoTracking()
                    .Where(eu => eu.UserId == currentUserId)
                    .Select(eu => eu.EnterpriseUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (enterpriseUserId == Guid.Empty)
                {
                    return false;
                }

                return await _unitOfWork.Repository<Project>().Query()
                    .AsNoTracking()
                    .AnyAsync(p => p.ProjectId == projectId &&
                        (
                            p.MentorId == enterpriseUserId ||
                            (p.InternshipId.HasValue && p.InternshipGroup != null && p.InternshipGroup.MentorId == enterpriseUserId)
                        ), cancellationToken);
            }

            var studentId = await _unitOfWork.Repository<Student>().Query()
                .AsNoTracking()
                .Where(s => s.UserId == currentUserId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentId == Guid.Empty)
            {
                return false;
            }

            var internshipId = await _unitOfWork.Repository<Project>().Query()
                .AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => p.InternshipId)
                .FirstOrDefaultAsync(cancellationToken);

            if (internshipId == Guid.Empty)
            {
                return false;
            }

            return await _unitOfWork.Repository<InternshipStudent>().Query()
                .AsNoTracking()
                .AnyAsync(m => m.InternshipId == internshipId && m.StudentId == studentId, cancellationToken);
        }

        private async Task<Guid?> ResolveMentorUploaderIdAsync(CancellationToken cancellationToken)
        {
            if (!string.Equals(_currentUserService.Role, "Mentor", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return null;
            }

            var enterpriseUserId = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .Where(eu => eu.UserId == currentUserId)
                .Select(eu => eu.EnterpriseUserId)
                .FirstOrDefaultAsync(cancellationToken);

            return enterpriseUserId == Guid.Empty ? null : enterpriseUserId;
        }

    }
}
