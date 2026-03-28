using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Common;
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

            bool hasData = await _unitOfWork.Repository<Logbook>().Query().AnyAsync(l => l.InternshipId == request.InternshipGroupId, cancellationToken) ||
                           await _unitOfWork.Repository<Evaluation>().Query().AnyAsync(e => e.InternshipId == request.InternshipGroupId, cancellationToken) ||
                           await _unitOfWork.Repository<ViolationReport>().Query().AnyAsync(v => v.InternshipGroupId == request.InternshipGroupId, cancellationToken) ||
                           await _unitOfWork.Repository<Project>().Query().AnyAsync(p => p.InternshipId == request.InternshipGroupId && (p.WorkItems.Any() || p.Sprints.Any() || p.ProjectResources.Any()), cancellationToken);

            if (!hasData)
            {
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.CannotArchiveNoData),
                    ResultErrorType.BadRequest);
            }

            group.UpdateStatus(GroupStatus.Archived);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(group);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Clear Cache
                await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(request.InternshipGroupId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);

                return Result<ArchiveInternshipGroupResponse>.Success(new ArchiveInternshipGroupResponse
                {
                    InternshipGroupId = request.InternshipGroupId,
                    Message = _messageService.GetMessage(MessageKeys.InternshipGroups.ArchiveSuccess)
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, MessageKeys.InternshipGroups.LogUpdateError);
                return Result<ArchiveInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}
