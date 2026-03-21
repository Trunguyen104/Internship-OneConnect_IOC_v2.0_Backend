using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.ProjectResources.Common;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.ProjectResources.Commands.DeleteProjectResource
{
    public class DeleteProjectResourceHandler : IRequestHandler<DeleteProjectResourceCommand, Result<DeleteProjectResourceResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteProjectResourceHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;
        public DeleteProjectResourceHandler(IUnitOfWork unitOfWork, ILogger<DeleteProjectResourceHandler> logger
            , IMapper mapper, IMessageService messageService, ICurrentUserService currentUserService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _mapper = mapper;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
        }

        public async Task<Result<DeleteProjectResourceResponse>> Handle(DeleteProjectResourceCommand request, CancellationToken cancellationToken)
        {
            // Check if the resource exists
            var resource = await _unitOfWork.Repository<Domain.Entities.ProjectResources>().GetByIdAsync(request.ResourceId);
            if (resource == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogProjectResourceNotFound), request.ResourceId);
                return Result<DeleteProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.NotFound), ResultErrorType.NotFound);
            }

            var hasAccess = await HasProjectAccessAsync(resource.ProjectId, cancellationToken);
            if (!hasAccess)
            {
                return Result<DeleteProjectResourceResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Forbidden),
                    ResultErrorType.Forbidden);
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Soft delete the resource properly via the Repository configuration
                await _unitOfWork.Repository<Domain.Entities.ProjectResources>().DeleteAsync(resource, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveAsync(ProjectResourceCacheKeys.Read(resource.ProjectResourceId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(ProjectResourceCacheKeys.ListPattern(resource.ProjectId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(ProjectResourceCacheKeys.ListPattern(null), cancellationToken);

                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.LogDeleteSuccess), request.ResourceId);
                var response = _mapper.Map<DeleteProjectResourceResponse>(resource);
                return Result<DeleteProjectResourceResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to delete project resource: {ResourceId}", request.ResourceId);
                return Result<DeleteProjectResourceResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
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
