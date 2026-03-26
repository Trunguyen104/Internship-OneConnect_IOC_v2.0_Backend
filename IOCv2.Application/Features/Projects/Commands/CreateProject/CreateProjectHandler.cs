using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
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
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateProjectHandler> _logger;
        private readonly IMessageService _message;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUser;
        private readonly IFileStorageService _fileStorage;

        public CreateProjectHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<CreateProjectHandler> logger,
            IMessageService message,
            ICacheService cacheService,
            ICurrentUserService currentUser,
            IFileStorageService fileStorage)
        {
            _unitOfWork   = unitOfWork;
            _mapper       = mapper;
            _logger       = logger;
            _message      = message;
            _cacheService = cacheService;
            _currentUser  = currentUser;
            _fileStorage  = fileStorage;
        }

        public async Task<Result<CreateProjectResponse>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogCreating), request.ProjectName, request.InternshipId);

            // 1. Parse current user
            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<CreateProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            // 2. Resolve EnterpriseUser (MentorId)
            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<CreateProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            // 3. Load InternshipGroup — chỉ check nếu request có InternshipId
            InternshipGroup? group = null;
            if (request.InternshipId.HasValue && request.InternshipId.Value != Guid.Empty)
            {
                group = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(g => g.Enterprise)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(g => g.InternshipId == request.InternshipId.Value, cancellationToken);

                if (group == null)
                    return Result<CreateProjectResponse>.Failure(_message.GetMessage(MessageKeys.Internships.NotFound), ResultErrorType.NotFound);

                if (group.Status == GroupStatus.Archived || group.Status == GroupStatus.Finished)
                    return Result<CreateProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.GroupNotActiveForCreate), ResultErrorType.BadRequest);

                // Scope check: chỉ mentor của group mới được tạo project
                if (group.MentorId != enterpriseUser.EnterpriseUserId)
                    return Result<CreateProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }

            // 4. Uniqueness check: ProjectName trong cùng group (nếu có group)
            if (request.InternshipId.HasValue && request.InternshipId.Value != Guid.Empty)
            {
                var nameExists = await _unitOfWork.Repository<Project>()
                    .ExistsAsync(p => p.InternshipId == request.InternshipId && p.ProjectName == request.ProjectName, cancellationToken);

                if (nameExists)
                {
                    _logger.LogWarning(_message.GetMessage(MessageKeys.Projects.LogNameExists), request.ProjectName);
                    return Result<CreateProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.ProjectNameExistsInternship), ResultErrorType.Conflict);
                }
            }

            // 5. Resolve ProjectCode
            string projectCode = request.ProjectCode?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(projectCode))
            {
                if (group != null)
                {
                    var totalProjects = await _unitOfWork.Repository<Project>().Query()
                        .IgnoreQueryFilters()
                        .CountAsync(p => p.InternshipId == request.InternshipId, cancellationToken);

                    const int maxRetries = 3;
                    bool found = false;
                    for (int attempt = 1; attempt <= maxRetries; attempt++)
                    {
                        var candidate = ProjectCodeGenerator.Generate(
                            group.Enterprise?.Name ?? string.Empty,
                            group.GroupName,
                            totalProjects + attempt);

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
                }
                else
                {
                    var totalGlobal = await _unitOfWork.Repository<Project>().Query()
                        .IgnoreQueryFilters()
                        .CountAsync(cancellationToken: cancellationToken);
                    projectCode = ProjectCodeGenerator.Generate(string.Empty, "STANDALONE", totalGlobal + 1);
                }
            }

            // 6. Validate + pre-upload files (trước transaction để tránh orphan upload nếu DB commit fail)
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

            // 7. Create + Persist
            var newProject = Project.Create(
                request.InternshipId, request.ProjectName, request.Description,
                projectCode, request.Field, request.Requirements,
                request.Deliverables, request.Template, enterpriseUser.EnterpriseUserId,
                request.StartDate, request.EndDate);

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
                        var fileUrl  = await _fileStorage.UploadFileAsync(
                            file, FileParams.GetFolder(newProject.ProjectId), fileName, cancellationToken);
                        uploadedFiles.Add((fileUrl, file.FileName, FileValidationHelper.GetFileType(fileUrl)!.Value));

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

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);
                _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogCreateSuccess), newProject.ProjectId, request.InternshipId);

                // Reload kèm resources nếu có đính kèm, ngược lại map trực tiếp từ entity
                if (uploadedFiles.Count > 0 || request.Links?.Count > 0)
                {
                    var createdProject = await _unitOfWork.Repository<Project>().Query()
                        .Include(p => p.ProjectResources)
                        .AsNoTracking()
                        .FirstAsync(p => p.ProjectId == newProject.ProjectId, cancellationToken);
                    return Result<CreateProjectResponse>.Success(_mapper.Map<CreateProjectResponse>(createdProject));
                }

                return Result<CreateProjectResponse>.Success(_mapper.Map<CreateProjectResponse>(newProject));
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
    }
}
