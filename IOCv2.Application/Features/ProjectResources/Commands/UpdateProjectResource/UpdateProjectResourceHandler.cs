using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.ProjectResources.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.UpdateProjectResource
{
    public class UpdateProjectResourceHandler : IRequestHandler<UpdateProjectResourceCommand, Result<UpdateProjectResourceResponse>>
    {
        private enum ProjectAccessLevel
        {
            None,
            Student,
            Mentor,
            Admin
        }

        private readonly ILogger<UpdateProjectResourceHandler> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public UpdateProjectResourceHandler(ILogger<UpdateProjectResourceHandler> logger, IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService, ICurrentUserService currentUserService, ICacheService cacheService)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
        }
        public async Task<Result<UpdateProjectResourceResponse>> Handle(UpdateProjectResourceCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Check if the project resource exists
                var projectResource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>().GetByIdAsync(request.ProjectResourceId);
                if (projectResource == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ProjectResourceId);
                    return Result<UpdateProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound), ResultErrorType.NotFound);
                }
                if (projectResource.ProjectId != request.ProjectId)
                {
                    return Result<UpdateProjectResourceResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.InvalidRequest),
                        ResultErrorType.BadRequest);
                }

                var accessLevel = await ResolveProjectAccessAsync(request.ProjectId, cancellationToken);
                if (accessLevel == ProjectAccessLevel.None)
                {
                    return Result<UpdateProjectResourceResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }

                // Student can only manage resources created by students (mentor uploads are protected).
                if (accessLevel == ProjectAccessLevel.Student && projectResource.UploadedBy.HasValue)
                {
                    return Result<UpdateProjectResourceResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.ProjectResourcesKey.StudentCannotModifyMentorResource),
                        ResultErrorType.Forbidden);
                }

                if (request.ResourceType != default && request.ResourceType != projectResource.ResourceType)
                {
                    return Result<UpdateProjectResourceResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.InvalidRequest),
                        ResultErrorType.BadRequest);
                }

                if (!IsResourceNameExtensionValid(request.ResourceName, projectResource.ResourceType))
                {
                    return Result<UpdateProjectResourceResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.ProjectResourcesKey.InvalidFileType),
                        ResultErrorType.BadRequest);
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Resource type and project binding are immutable; only allow metadata rename.
                projectResource.UpdateInfo(projectResource.ProjectId, request.ResourceName, projectResource.ResourceType);

                
                await _unitOfWork.Repository<Domain.Entities.ProjectResources>().UpdateAsync(projectResource, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveAsync(ProjectResourceCacheKeys.Read(projectResource.ProjectResourceId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(ProjectResourceCacheKeys.ListPattern(projectResource.ProjectId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(ProjectResourceCacheKeys.ListPattern(null), cancellationToken);

                var response = _mapper.Map<UpdateProjectResourceResponse>(projectResource);
                return Result<UpdateProjectResourceResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogUpdateError), request.ProjectResourceId);
                return Result<UpdateProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.UpdateError), ResultErrorType.InternalServerError);
            }
        }

        private async Task<ProjectAccessLevel> ResolveProjectAccessAsync(Guid projectId, CancellationToken cancellationToken)
        {
            if (string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_currentUserService.Role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                return ProjectAccessLevel.Admin;
            }

            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return ProjectAccessLevel.None;
            }

            if (string.Equals(_currentUserService.Role, "Mentor", StringComparison.OrdinalIgnoreCase))
            {
                var enterpriseUserId = await _unitOfWork.Repository<Domain.Entities.EnterpriseUser>().Query()
                    .AsNoTracking()
                    .Where(eu => eu.UserId == currentUserId)
                    .Select(eu => eu.EnterpriseUserId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (enterpriseUserId == Guid.Empty)
                {
                    return ProjectAccessLevel.None;
                }

                var mentorHasAccess = await _unitOfWork.Repository<Domain.Entities.Project>().Query()
                    .AsNoTracking()
                    .AnyAsync(p => p.ProjectId == projectId &&
                        (
                            p.MentorId == enterpriseUserId ||
                            (p.InternshipId.HasValue && p.InternshipGroup != null && p.InternshipGroup.MentorId == enterpriseUserId)
                        ), cancellationToken);

                return mentorHasAccess ? ProjectAccessLevel.Mentor : ProjectAccessLevel.None;
            }

            var studentId = await _unitOfWork.Repository<Domain.Entities.Student>().Query()
                .AsNoTracking()
                .Where(s => s.UserId == currentUserId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentId == Guid.Empty)
            {
                return ProjectAccessLevel.None;
            }

            var internshipId = await _unitOfWork.Repository<Domain.Entities.Project>().Query()
                .AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => p.InternshipId)
                .FirstOrDefaultAsync(cancellationToken);

            if (internshipId == Guid.Empty)
            {
                return ProjectAccessLevel.None;
            }

            var studentHasAccess = await _unitOfWork.Repository<Domain.Entities.InternshipStudent>().Query()
                .AsNoTracking()
                .AnyAsync(m => m.InternshipId == internshipId && m.StudentId == studentId, cancellationToken);

            return studentHasAccess ? ProjectAccessLevel.Student : ProjectAccessLevel.None;
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
