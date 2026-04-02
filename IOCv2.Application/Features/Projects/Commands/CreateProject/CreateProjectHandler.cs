using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.ProjectResources;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Features.Projects.Queries.GetProjectById;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Commands.CreateProject
{
    public class CreateProjectHandler : IRequestHandler<CreateProjectCommand, Result<CreateProjectResponse>>
    {
        private const string DefaultProjectField = "Software Engineering";

        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateProjectHandler> _logger;
        private readonly IMessageService _message;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUser;
        private readonly IFileStorageService _fileStorage;
        private readonly INotificationPushService _pushService;

        public CreateProjectHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateProjectHandler> logger,
            IMessageService message,
            ICacheService cacheService,
            ICurrentUserService currentUser,
            IFileStorageService fileStorage,
            INotificationPushService pushService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _message = message;
            _cacheService = cacheService;
            _currentUser = currentUser;
            _fileStorage = fileStorage;
            _pushService = pushService;
        }

        public async Task<Result<CreateProjectResponse>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogCreating), request.ProjectName);

            // 1. Parse current user
            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<CreateProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            // 2. Resolve EnterpriseUser (MentorId) — include Enterprise for code generation
            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .Include(eu => eu.Enterprise)
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<CreateProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            // 3. Resolve ProjectCode — fallback to enterprise name when no group
            string projectCode;

            var totalGlobal = await _unitOfWork.Repository<Project>().Query()
                .IgnoreQueryFilters()
                .CountAsync(cancellationToken: cancellationToken);

            const int maxRetries = 3;
            bool found = false;
            projectCode = string.Empty;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var candidate = ProjectCodeGenerator.Generate(
                    enterpriseUser.Enterprise?.Name ?? string.Empty,
                    string.Empty,  // không có group name
                    totalGlobal + attempt);

                var codeExists = await _unitOfWork.Repository<Project>().Query()
                    .IgnoreQueryFilters()
                    .AnyAsync(p => p.ProjectCode == candidate, cancellationToken);

                if (!codeExists)
                {
                    projectCode = candidate;
                    found = true;
                    break;
                }
            }
            if (!found)
                return Result<CreateProjectResponse>.Failure(
                    _message.GetMessage(MessageKeys.Projects.ProjectCodeConflict),
                    ResultErrorType.Conflict);

            // 4. Validate + pre-upload files (trước transaction để tránh orphan upload nếu DB commit fail)
            var uploadedFiles = new List<(string url, string originalName, FileType type)>();
            if (request.Files?.Count > 0)
            {
                foreach (var file in request.Files)
                {
                    var validation = FileValidationHelper.ValidateFile(file.FileName, file.Length);
                    if (!validation.IsValid)
                        return Result<CreateProjectResponse>.Failure(
                            $"{file.FileName}: {validation.ErrorMessage}",
                            ResultErrorType.BadRequest);
                }
            }

            // 5. F1/F2: Validate optional group assignment trước khi tạo project
            InternshipGroup? assignedGroup = null;
            if (request.InternshipGroupId.HasValue)
            {
                assignedGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.InternshipId == request.InternshipGroupId.Value, cancellationToken);

                if (assignedGroup == null)
                    return Result<CreateProjectResponse>.Failure(
                        _message.GetMessage(MessageKeys.Internships.NotFound), ResultErrorType.NotFound);

                if (assignedGroup.Status == GroupStatus.Archived)
                    return Result<CreateProjectResponse>.Failure(
                        _message.GetMessage(MessageKeys.Projects.CannotAssignArchivedGroup), ResultErrorType.BadRequest);

                if (assignedGroup.Status == GroupStatus.Finished)
                    return Result<CreateProjectResponse>.Failure(
                        _message.GetMessage(MessageKeys.Projects.GroupNotActive), ResultErrorType.BadRequest);

                if (assignedGroup.EndDate.HasValue && assignedGroup.EndDate.Value.Date < DateTime.UtcNow.Date)
                    return Result<CreateProjectResponse>.Failure(
                        _message.GetMessage(MessageKeys.Projects.GroupPhaseEnded), ResultErrorType.BadRequest);

                if (assignedGroup.MentorId != enterpriseUser.EnterpriseUserId)
                    return Result<CreateProjectResponse>.Failure(
                        _message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }

            // 6. Create + Persist
            var projectRequirements = string.IsNullOrWhiteSpace(request.Requirements)
                ? string.Empty
                : request.Requirements.Trim();

            var newProject = Project.Create(
                request.ProjectName, request.Description,
                projectCode, DefaultProjectField, projectRequirements,
                request.Deliverables, mentorId: enterpriseUser.EnterpriseUserId,
                startDate: request.StartDate, endDate: request.EndDate);

            // F2: PublishOnSave — Mentor nhấn Save → Published ngay khi tạo
            if (request.PublishOnSave)
                newProject.Publish();

            // F2: AssignToGroup nếu có InternshipGroupId
            if (assignedGroup != null)
                newProject.AssignToGroup(assignedGroup.InternshipId, assignedGroup.StartDate, assignedGroup.EndDate);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<Project>().AddAsync(newProject, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Upload files và tạo ProjectResources records
                if (request.Files?.Count > 0)
                {
                    foreach (var file in request.Files)
                    {
                        var fileName = FileParams.GetFileName(file.FileName);
                        var fileUrl = await _fileStorage.UploadFileAsync(
                            file, FileParams.GetFolder(newProject.ProjectId), fileName, cancellationToken);
                        uploadedFiles.Add((fileUrl, file.FileName, FileValidationHelper.GetFileType(file.FileName)!.Value));

                        var resource = new IOCv2.Domain.Entities.ProjectResources(newProject.ProjectId, file.FileName, uploadedFiles[^1].type, fileUrl);
                        await _unitOfWork.Repository<IOCv2.Domain.Entities.ProjectResources>().AddAsync(resource, cancellationToken);
                    }
                }

                // Tạo ProjectResources records cho link đính kèm
                if (request.Links?.Count > 0)
                {
                    foreach (var link in request.Links.Where(l => !string.IsNullOrWhiteSpace(l.Url)))
                    {
                        var resource = new IOCv2.Domain.Entities.ProjectResources(
                            newProject.ProjectId,
                            string.IsNullOrWhiteSpace(link.ResourceName) ? link.Url : link.ResourceName,
                            FileType.LINK,
                            link.Url.Trim());
                        await _unitOfWork.Repository<IOCv2.Domain.Entities.ProjectResources>().AddAsync(resource, cancellationToken);
                    }
                }

                if (request.Files?.Count > 0 || request.Links?.Count > 0)
                    await _unitOfWork.SaveChangeAsync(cancellationToken);

                // F2: Notify students nếu project Published + có group assigned
                if (assignedGroup != null && newProject.VisibilityStatus == VisibilityStatus.Published)
                {
                    var studentUserIds = await _unitOfWork.Repository<InternshipStudent>().Query()
                        .Where(s => s.InternshipId == assignedGroup.InternshipId)
                        .Select(s => s.Student.UserId)
                        .ToListAsync(cancellationToken);

                    foreach (var userId in studentUserIds)
                    {
                        var notif = new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId = userId,
                            Title = _message.GetMessage(MessageKeys.Projects.NotifNewProjectTitle),
                            Content = _message.GetMessage(MessageKeys.Projects.NotifNewProjectContent, newProject.ProjectName),
                            Type = NotificationType.General,
                            ReferenceType = "Project",
                            ReferenceId = newProject.ProjectId
                        };
                        await _unitOfWork.Repository<Notification>().AddAsync(notif, cancellationToken);
                    }

                    if (studentUserIds.Any())
                        await _unitOfWork.SaveChangeAsync(cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);
                _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogCreateSuccess), newProject.ProjectId);

                // AC-13: Push ProjectListChanged signal tới Mentor
                if (Guid.TryParse(_currentUser.UserId, out var mentorUserIdForSignal))
                {
                    try
                    {
                        await _pushService.PushNewNotificationAsync(mentorUserIdForSignal, new
                        {
                            type = ProjectSignalConstants.ProjectListChanged,
                            action = ProjectSignalConstants.Actions.Created,
                            projectId = newProject.ProjectId
                        }, cancellationToken);
                        _logger.LogInformation(
                            _message.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                            ProjectSignalConstants.Actions.Created, mentorUserIdForSignal, newProject.ProjectId);
                    }
                    catch (Exception signalEx)
                    {
                        _logger.LogWarning(signalEx, _message.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                            ProjectSignalConstants.Actions.Created, mentorUserIdForSignal, newProject.ProjectId);
                    }
                }

                // Reload kèm resources nếu có đính kèm, ngược lại map trực tiếp từ entity
                if (uploadedFiles.Count > 0 || request.Links?.Count > 0)
                {
                    var createdProject = await _unitOfWork.Repository<Project>().Query()
                        .Include(p => p.ProjectResources)
                        .AsNoTracking()
                        .FirstAsync(p => p.ProjectId == newProject.ProjectId, cancellationToken);
                    var createdResponse = _mapper.Map<CreateProjectResponse>(createdProject);
                    ResolveResourceUrls(createdResponse.ProjectResources);
                    return Result<CreateProjectResponse>.Success(createdResponse);
                }

                var response = _mapper.Map<CreateProjectResponse>(newProject);
                ResolveResourceUrls(response.ProjectResources);
                return Result<CreateProjectResponse>.Success(response);
            }
            catch (DbUpdateException dbEx) when (
                dbEx.InnerException?.Message.Contains("uix_projects_project_code_active") == true ||
                dbEx.InnerException?.Message.Contains("project_code") == true)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                // Dọn dẹp file đã upload nếu DB fail
                foreach (var (url, _, _) in uploadedFiles)
                    await _fileStorage.DeleteFileAsync(url, cancellationToken);
                _logger.LogWarning(_message.GetMessage(MessageKeys.Projects.LogCodeConflict), projectCode);
                return Result<CreateProjectResponse>.Failure(
                    _message.GetMessage(MessageKeys.Projects.ProjectCodeConflict),
                    ResultErrorType.Conflict);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                // Dọn dẹp file đã upload nếu DB fail
                foreach (var (url, _, _) in uploadedFiles)
                    await _fileStorage.DeleteFileAsync(url, cancellationToken);
                _logger.LogError(ex, _message.GetMessage(MessageKeys.Projects.LogCreateError));
                return Result<CreateProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }

        private void ResolveResourceUrls(List<ProjectResourcesDTO> resources)
        {
            if (resources.Count == 0)
            {
                return;
            }

            foreach (var resource in resources)
            {
                if (resource.ResourceType == FileType.LINK || string.IsNullOrWhiteSpace(resource.ResourceUrl))
                {
                    continue;
                }

                resource.ResourceUrl = _fileStorage.GetFileUrl(resource.ResourceUrl);
            }
        }
    }
}