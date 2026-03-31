using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Commands.UnpublishProject
{
    public class UnpublishProjectHandler : IRequestHandler<UnpublishProjectCommand, Result<UnpublishProjectResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IMessageService _message;
        private readonly ICacheService _cacheService;
        private readonly ILogger<UnpublishProjectHandler> _logger;
        private readonly INotificationPushService _pushService;

        public UnpublishProjectHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser,
            IMessageService message, ICacheService cacheService, ILogger<UnpublishProjectHandler> logger,
            INotificationPushService pushService)
        {
            _unitOfWork   = unitOfWork;
            _currentUser  = currentUser;
            _message      = message;
            _cacheService = cacheService;
            _logger       = logger;
            _pushService  = pushService;
        }

        public async Task<Result<UnpublishProjectResponse>> Handle(UnpublishProjectCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<UnpublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<UnpublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            var project = await _unitOfWork.Repository<Project>().Query()
                .Include(p => p.InternshipGroup)
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
                return Result<UnpublishProjectResponse>.NotFound(_message.GetMessage(MessageKeys.Projects.NotFound));

            var canManageProject = project.InternshipId.HasValue
                ? project.InternshipGroup?.MentorId == enterpriseUser.EnterpriseUserId
                : project.MentorId == enterpriseUser.EnterpriseUserId;

            if (!canManageProject)
                return Result<UnpublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            // Phải đang Published
            if (project.VisibilityStatus != VisibilityStatus.Published)
                return Result<UnpublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.InvalidStatusForUnpublish), ResultErrorType.BadRequest);

            // Chỉ unpublish được khi OperationalStatus == Unstarted
            if (project.OperationalStatus != OperationalStatus.Unstarted)
                return Result<UnpublishProjectResponse>.Failure(
                    _message.GetMessage(MessageKeys.Projects.CannotUnpublishStarted),
                    ResultErrorType.BadRequest);

            project.Unpublish();

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

                _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogUnpublishSuccess), project.ProjectId);

                // AC-13: Push ProjectListChanged signal tới Mentor
                if (Guid.TryParse(_currentUser.UserId, out var mentorUserIdForSignal))
                {
                    try
                    {
                        await _pushService.PushNewNotificationAsync(mentorUserIdForSignal, new
                        {
                            type      = ProjectSignalConstants.ProjectListChanged,
                            action    = ProjectSignalConstants.Actions.Unpublished,
                            projectId = project.ProjectId
                        }, cancellationToken);
                        _logger.LogInformation(
                            _message.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                            ProjectSignalConstants.Actions.Unpublished, mentorUserIdForSignal, project.ProjectId);
                    }
                    catch (Exception signalEx)
                    {
                        _logger.LogWarning(signalEx, _message.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                            ProjectSignalConstants.Actions.Unpublished, mentorUserIdForSignal, project.ProjectId);
                    }
                }

                return Result<UnpublishProjectResponse>.Success(new UnpublishProjectResponse
                {
                    ProjectId         = project.ProjectId,
                    VisibilityStatus  = project.VisibilityStatus,
                    OperationalStatus = project.OperationalStatus,
                    UpdatedAt         = project.UpdatedAt ?? DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _message.GetMessage(MessageKeys.Projects.LogUnpublishError), project.ProjectId);
                return Result<UnpublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
