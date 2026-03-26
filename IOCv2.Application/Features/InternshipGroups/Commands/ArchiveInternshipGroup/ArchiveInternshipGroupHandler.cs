using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Common;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Commands.ArchiveInternshipGroup
{
    public class ArchiveInternshipGroupHandler : IRequestHandler<ArchiveInternshipGroupCommand, Result<ArchiveInternshipGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<ArchiveInternshipGroupHandler> _logger;
        private readonly ICacheService _cacheService;

        public ArchiveInternshipGroupHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<ArchiveInternshipGroupHandler> logger,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<ArchiveInternshipGroupResponse>> Handle(ArchiveInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);
            
            if (enterpriseUser == null)
            {
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                .FirstOrDefaultAsync(g => g.InternshipId == request.InternshipGroupId, cancellationToken);

            if (group == null)
            {
                return Result<ArchiveInternshipGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
            }

            if (group.EnterpriseId != enterpriseUser.EnterpriseId)
            {
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise),
                    ResultErrorType.Forbidden);
            }

            if (group.Status == GroupStatus.Archived)
            {
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.GroupAlreadyArchived),
                    ResultErrorType.BadRequest);
            }

            group.UpdateStatus(GroupStatus.Archived);

            // Lấy project IDs cần archive trước để invalidate cache sau commit
            var projectIdsToArchive = await _unitOfWork.Repository<Project>().Query()
                .Where(p => p.InternshipId == request.InternshipGroupId
                         && (p.Status == ProjectStatus.Draft || p.Status == ProjectStatus.Published))
                .Select(p => p.ProjectId)
                .ToListAsync(cancellationToken);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(group);

                // Batch archive Draft/Published projects trong group này
                var archivedCount = await _unitOfWork.Repository<Project>().ExecuteUpdateAsync(
                    p => p.InternshipId == request.InternshipGroupId
                         && (p.Status == ProjectStatus.Draft || p.Status == ProjectStatus.Published),
                    s => s.SetProperty(p => p.Status, ProjectStatus.Archived)
                          .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);

                if (archivedCount > 0)
                    _logger.LogInformation(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.LogProjectsArchived),
                        archivedCount, request.InternshipGroupId);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogUpdateError));
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }

            // Post-commit cache invalidation
            await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(request.InternshipGroupId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);
            await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);
            foreach (var projectId in projectIdsToArchive)
                await _cacheService.RemoveAsync(ProjectCacheKeys.Project(projectId), cancellationToken);

            return Result<ArchiveInternshipGroupResponse>.Success(new ArchiveInternshipGroupResponse
            {
                InternshipGroupId = request.InternshipGroupId,
                Message = _messageService.GetMessage(MessageKeys.InternshipGroups.ArchiveSuccess)
            });
        }
    }
}
