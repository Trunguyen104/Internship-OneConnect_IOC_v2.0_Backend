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

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    public class UpdateProjectHandler : IRequestHandler<UpdateProjectCommand, Result<UpdateProjectResponse>>
    {
        private const string FixedProjectField = "Software Engineering";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateProjectHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUser;
        private readonly ICacheService _cacheService;
        private readonly INotificationPushService _pushService;
        private readonly IFileStorageService? _fileStorageService;

        public UpdateProjectHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UpdateProjectHandler> logger,
            IMessageService messageService, ICurrentUserService currentUser, ICacheService cacheService,
            INotificationPushService pushService, IFileStorageService? fileStorageService = null)
        {
            _unitOfWork     = unitOfWork;
            _mapper         = mapper;
            _logger         = logger;
            _messageService = messageService;
            _currentUser    = currentUser;
            _cacheService   = cacheService;
            _pushService    = pushService;
            _fileStorageService = fileStorageService;
        }

        public async Task<Result<UpdateProjectResponse>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogUpdating), request.ProjectId, _currentUser.UserId);

            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            var project = await _unitOfWork.Repository<Project>().Query()
                .Include(p => p.InternshipGroup)
                .Include(p => p.ProjectResources)
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Projects.LogNotFound), request.ProjectId);
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.NotFound), ResultErrorType.NotFound);
            }

            var canManageProject = project.InternshipId.HasValue
                ? project.InternshipGroup?.MentorId == enterpriseUser.EnterpriseUserId
                : project.MentorId == enterpriseUser.EnterpriseUserId;

            if (!canManageProject)
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            // Block nếu project không còn editable (OperationalStatus không phải Unstarted/Active)
            if (!project.IsEditable)
                return Result<UpdateProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Projects.InvalidStatusForUpdate), ResultErrorType.BadRequest);

            InternshipGroup? targetGroup = null;
            var hasGroupChangeRequested = request.InternshipGroupId.HasValue
                                          && request.InternshipGroupId.Value != project.InternshipId;

            if (hasGroupChangeRequested)
            {
                targetGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.InternshipId == request.InternshipGroupId!.Value, cancellationToken);

                if (targetGroup == null)
                    return Result<UpdateProjectResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Internships.NotFound), ResultErrorType.NotFound);

                if (targetGroup.Status == GroupStatus.Archived)
                    return Result<UpdateProjectResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Projects.CannotAssignArchivedGroup), ResultErrorType.BadRequest);

                if (targetGroup.Status == GroupStatus.Finished)
                    return Result<UpdateProjectResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Projects.GroupNotActive), ResultErrorType.BadRequest);

                if (targetGroup.EndDate.HasValue && targetGroup.EndDate.Value.Date < DateTime.UtcNow.Date)
                    return Result<UpdateProjectResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Projects.GroupPhaseEnded), ResultErrorType.BadRequest);

                if (!targetGroup.MentorId.HasValue)
                    return Result<UpdateProjectResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Projects.GroupHasNoMentor), ResultErrorType.BadRequest);

                if (targetGroup.MentorId != enterpriseUser.EnterpriseUserId)
                    return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                var hasActiveProjectInTargetGroup = await _unitOfWork.Repository<Project>().Query()
                    .AnyAsync(p => p.InternshipId == targetGroup.InternshipId
                                && p.ProjectId != project.ProjectId
                                && p.OperationalStatus == OperationalStatus.Active,
                        cancellationToken);

                if (hasActiveProjectInTargetGroup)
                    return Result<UpdateProjectResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Projects.GroupAlreadyHasActiveProject),
                        ResultErrorType.Conflict);
            }

            var assignedCount = 0;
            var resourceReadCacheKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var filesToDelete = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var uploadedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Uniqueness check: ProjectName nếu thay đổi
                if (request.ProjectName is not null && project.ProjectName != request.ProjectName)
                {
                    var nameExists = await _unitOfWork.Repository<Project>()
                        .ExistsAsync(p => p.InternshipId == project.InternshipId
                                       && p.ProjectName == request.ProjectName
                                       && p.ProjectId != request.ProjectId, cancellationToken);
                    if (nameExists)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<UpdateProjectResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Projects.ProjectNameExistsInternship), ResultErrorType.Conflict);
                    }
                }

                project.Update(
                    request.ProjectName,
                    request.Description,
                    request.StartDate,
                    request.EndDate,
                    FixedProjectField,
                    request.Requirements,
                    request.Deliverables,
                    request.Template);

                if (targetGroup != null)
                    project.AssignToGroup(targetGroup.InternshipId, targetGroup.StartDate, targetGroup.EndDate);

                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (request.Files?.Count > 0)
                {
                    if (_fileStorageService == null)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<UpdateProjectResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Common.InternalError),
                            ResultErrorType.InternalServerError);
                    }

                    foreach (var file in request.Files)
                    {
                        var validation = FileValidationHelper.ValidateFile(file.FileName, file.Length);
                        if (!validation.IsValid)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            return Result<UpdateProjectResponse>.Failure(
                                $"{file.FileName}: {validation.ErrorMessage}",
                                ResultErrorType.BadRequest);
                        }

                        var fileName = FileParams.GetFileName(file.FileName);
                        var fileUrl = await _fileStorageService.UploadFileAsync(
                            file,
                            FileParams.GetFolder(project.ProjectId),
                            fileName,
                            cancellationToken);
                        uploadedFiles.Add(fileUrl);

                        var fileType = FileValidationHelper.GetFileType(file.FileName);
                        if (!fileType.HasValue)
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            return Result<UpdateProjectResponse>.Failure(
                                _messageService.GetMessage(MessageKeys.ProjectResourcesKey.InvalidFileType),
                                ResultErrorType.BadRequest);
                        }

                        var resource = new IOCv2.Domain.Entities.ProjectResources(
                            project.ProjectId,
                            file.FileName,
                            fileType.Value,
                            fileUrl);

                        await _unitOfWork.Repository<IOCv2.Domain.Entities.ProjectResources>().AddAsync(resource, cancellationToken);
                        resourceReadCacheKeys.Add(ProjectResourceCacheKeys.Read(resource.ProjectResourceId));
                    }
                }

                if (request.Links?.Count > 0)
                {
                    foreach (var link in request.Links.Where(l => !string.IsNullOrWhiteSpace(l.Url)))
                    {
                        var resource = new IOCv2.Domain.Entities.ProjectResources(
                            project.ProjectId,
                            string.IsNullOrWhiteSpace(link.ResourceName) ? link.Url : link.ResourceName,
                            FileType.LINK,
                            link.Url.Trim());

                        await _unitOfWork.Repository<IOCv2.Domain.Entities.ProjectResources>().AddAsync(resource, cancellationToken);
                        resourceReadCacheKeys.Add(ProjectResourceCacheKeys.Read(resource.ProjectResourceId));
                    }
                }

                var resourceById = project.ProjectResources
                    .ToDictionary(r => r.ProjectResourceId, r => r);

                if (request.ResourceDeleteIds?.Count > 0)
                {
                    foreach (var resourceId in request.ResourceDeleteIds)
                    {
                        if (!resourceById.TryGetValue(resourceId, out var resource))
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            return Result<UpdateProjectResponse>.Failure(
                                _messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound),
                                ResultErrorType.NotFound);
                        }

                        await _unitOfWork.Repository<IOCv2.Domain.Entities.ProjectResources>().DeleteAsync(resource, cancellationToken);
                        resourceReadCacheKeys.Add(ProjectResourceCacheKeys.Read(resource.ProjectResourceId));

                        if (resource.ResourceType != FileType.LINK && !string.IsNullOrWhiteSpace(resource.ResourceUrl))
                        {
                            filesToDelete.Add(resource.ResourceUrl);
                        }

                        resourceById.Remove(resourceId);
                    }
                }

                if (request.ResourceUpdates?.Count > 0)
                {
                    foreach (var resourceInput in request.ResourceUpdates)
                    {
                        if (!resourceById.TryGetValue(resourceInput.ProjectResourceId, out var resource))
                        {
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                            return Result<UpdateProjectResponse>.Failure(
                                _messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound),
                                ResultErrorType.NotFound);
                        }

                        if (resourceInput.ResourceName != null)
                        {
                            var trimmedResourceName = resourceInput.ResourceName.Trim();
                            if (!IsResourceNameExtensionValid(trimmedResourceName, resource.ResourceType))
                            {
                                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                                return Result<UpdateProjectResponse>.Failure(
                                    _messageService.GetMessage(MessageKeys.ProjectResourcesKey.InvalidFileType),
                                    ResultErrorType.BadRequest);
                            }

                            resource.ResourceName = trimmedResourceName;
                        }

                        if (resourceInput.ExternalUrl != null)
                        {
                            if (resource.ResourceType != FileType.LINK)
                            {
                                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                                return Result<UpdateProjectResponse>.Failure(
                                    _messageService.GetMessage(MessageKeys.Common.InvalidRequest),
                                    ResultErrorType.BadRequest);
                            }

                            resource.ResourceUrl = resourceInput.ExternalUrl.Trim();
                        }

                        await _unitOfWork.Repository<IOCv2.Domain.Entities.ProjectResources>().UpdateAsync(resource, cancellationToken);
                        resourceReadCacheKeys.Add(ProjectResourceCacheKeys.Read(resource.ProjectResourceId));
                    }
                }

                if ((request.ResourceDeleteIds?.Count > 0) || (request.ResourceUpdates?.Count > 0))
                {
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                }

                if ((request.Files?.Count > 0) || (request.Links?.Count > 0))
                {
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Notify students nếu project đang assigned cho group
                if (project.InternshipId.HasValue)
                {
                    var studentUserIds = await _unitOfWork.Repository<InternshipStudent>().Query()
                        .Where(s => s.InternshipId == project.InternshipId.Value)
                        .Select(s => s.Student.UserId)
                        .ToListAsync(cancellationToken);

                    foreach (var userId in studentUserIds)
                    {
                        var notif = new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId         = userId,
                            Title          = _messageService.GetMessage(MessageKeys.Projects.NotifUpdatedTitle),
                            Content        = _messageService.GetMessage(MessageKeys.Projects.NotifUpdatedContent, project.ProjectName),
                            Type           = NotificationType.General,
                            ReferenceType  = "Project",
                            ReferenceId    = project.ProjectId
                        };
                        await _unitOfWork.Repository<Notification>().AddAsync(notif, cancellationToken);
                    }

                    if (studentUserIds.Any())
                        await _unitOfWork.SaveChangeAsync(cancellationToken);
                }
            }
            catch (DbUpdateException dbEx) when (
                dbEx.InnerException?.Message.Contains("uix_projects_internship_id_active") == true)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                if (_fileStorageService != null && uploadedFiles.Count > 0)
                {
                    foreach (var uploadedFile in uploadedFiles)
                    {
                        try
                        {
                            await _fileStorageService.DeleteFileAsync(uploadedFile, cancellationToken);
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogWarning(cleanupEx, _messageService.GetMessage(MessageKeys.Projects.LogCleanupResourceFailed), uploadedFile);
                        }
                    }
                }

                return Result<UpdateProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Projects.GroupAlreadyHasActiveProject),
                    ResultErrorType.Conflict);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                if (_fileStorageService != null && uploadedFiles.Count > 0)
                {
                    foreach (var uploadedFile in uploadedFiles)
                    {
                        try
                        {
                            await _fileStorageService.DeleteFileAsync(uploadedFile, cancellationToken);
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogWarning(cleanupEx, _messageService.GetMessage(MessageKeys.Projects.LogCleanupResourceFailed), uploadedFile);
                        }
                    }
                }
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Projects.LogUpdateError), request.ProjectId);
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }

            if (_fileStorageService != null && filesToDelete.Count > 0)
            {
                foreach (var fileUrl in filesToDelete)
                {
                    try
                    {
                        await _fileStorageService.DeleteFileAsync(fileUrl, cancellationToken);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, _messageService.GetMessage(MessageKeys.Projects.LogDeleteResourceAfterUpdate), fileUrl);
                    }
                }
            }

            await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);
            await _cacheService.RemoveByPatternAsync(ProjectResourceCacheKeys.ListPattern(project.ProjectId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(ProjectResourceCacheKeys.ListPattern(null), cancellationToken);

            foreach (var readCacheKey in resourceReadCacheKeys)
            {
                await _cacheService.RemoveAsync(readCacheKey, cancellationToken);
            }

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogUpdateSuccess), request.ProjectId);

            // AC-13: Push ProjectListChanged signal tới Mentor
            if (Guid.TryParse(_currentUser.UserId, out var mentorUserIdForSignal))
            {
                try
                {
                    await _pushService.PushNewNotificationAsync(mentorUserIdForSignal, new
                    {
                        type      = ProjectSignalConstants.ProjectListChanged,
                        action    = ProjectSignalConstants.Actions.Updated,
                        projectId = project.ProjectId
                    }, cancellationToken);
                    _logger.LogInformation(
                        _messageService.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                        ProjectSignalConstants.Actions.Updated, mentorUserIdForSignal, project.ProjectId);
                }
                catch (Exception signalEx)
                {
                    _logger.LogWarning(signalEx, _messageService.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                        ProjectSignalConstants.Actions.Updated, mentorUserIdForSignal, project.ProjectId);
                }
            }

            // Đếm sinh viên trong group
            if (project.InternshipId.HasValue)
            {
                assignedCount = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .CountAsync(s => s.InternshipId == project.InternshipId.Value, cancellationToken);
            }

            var response = _mapper.Map<UpdateProjectResponse>(project);
            response.AssignedStudentCount = assignedCount;

            return Result<UpdateProjectResponse>.Success(response);
        }

        private static bool IsResourceNameExtensionValid(string? resourceName, FileType resourceType)
        {
            if (resourceType == FileType.LINK || string.IsNullOrWhiteSpace(resourceName))
            {
                return true;
            }

            var extension = Path.GetExtension(resourceName.Trim());
            if (string.IsNullOrWhiteSpace(extension))
            {
                return true;
            }

            if (resourceType == FileType.JPG)
            {
                return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                    || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
            }

            var expectedExtension = resourceType switch
            {
                FileType.PDF => ".pdf",
                FileType.DOCX => ".docx",
                FileType.PPTX => ".pptx",
                FileType.ZIP => ".zip",
                FileType.RAR => ".rar",
                FileType.PNG => ".png",
                FileType.XLSX => ".xlsx",
                _ => string.Empty
            };

            return string.IsNullOrEmpty(expectedExtension)
                || extension.Equals(expectedExtension, StringComparison.OrdinalIgnoreCase);
        }
    }
}
