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
                var projectExists = await _unitOfWork.Repository<Domain.Entities.Project>().ExistsAsync(p => p.ProjectId == request.ProjectId, cancellationToken);
                if (!projectExists)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.Projects.LogNotFound), request.ProjectId);
                    return Result<UpdateProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.NotFound), ResultErrorType.NotFound);
                }

                var hasAccess = await HasProjectAccessAsync(request.ProjectId, cancellationToken);
                if (!hasAccess)
                {
                    return Result<UpdateProjectResourceResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Update the project resource properties
                projectResource.UpdateInfo(request.ProjectId, request.ResourceName, request.ResourceType);

                
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

            var studentId = await _unitOfWork.Repository<Domain.Entities.Student>().Query()
                .AsNoTracking()
                .Where(s => s.UserId == currentUserId)
                .Select(s => s.StudentId)
                .FirstOrDefaultAsync(cancellationToken);

            if (studentId == Guid.Empty)
            {
                return false;
            }

            var internshipId = await _unitOfWork.Repository<Domain.Entities.Project>().Query()
                .AsNoTracking()
                .Where(p => p.ProjectId == projectId)
                .Select(p => p.InternshipId)
                .FirstOrDefaultAsync(cancellationToken);

            if (internshipId == Guid.Empty)
            {
                return false;
            }

            return await _unitOfWork.Repository<Domain.Entities.InternshipStudent>().Query()
                .AsNoTracking()
                .AnyAsync(m => m.InternshipId == internshipId && m.StudentId == studentId, cancellationToken);
        }
    }
}
