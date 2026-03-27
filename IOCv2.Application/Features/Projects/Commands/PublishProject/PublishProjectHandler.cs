using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Commands.PublishProject
{
    public class PublishProjectHandler : IRequestHandler<PublishProjectCommand, Result<PublishProjectResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IMessageService _message;
        private readonly ICacheService _cacheService;
        private readonly ILogger<PublishProjectHandler> _logger;
        private readonly INotificationPushService _pushService;

        public PublishProjectHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IMessageService message,
            ICacheService cacheService,
            ILogger<PublishProjectHandler> logger,
            INotificationPushService pushService)
        {
            _unitOfWork   = unitOfWork;
            _currentUser  = currentUser;
            _message      = message;
            _cacheService = cacheService;
            _logger       = logger;
            _pushService  = pushService;
        }

        public async Task<Result<PublishProjectResponse>> Handle(PublishProjectCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<PublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<PublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            var project = await _unitOfWork.Repository<Project>().Query()
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
                return Result<PublishProjectResponse>.NotFound(_message.GetMessage(MessageKeys.Projects.NotFound));

            // Scope check: phải là mentor của project
            if (project.MentorId != enterpriseUser.EnterpriseUserId)
                return Result<PublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            // Status check
            if (project.VisibilityStatus != VisibilityStatus.Draft)
                return Result<PublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.InvalidStatusForPublish), ResultErrorType.BadRequest);

            // Re-validate required fields
            if (string.IsNullOrWhiteSpace(project.Field) || string.IsNullOrWhiteSpace(project.Requirements))
                return Result<PublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.RequirementsRequired), ResultErrorType.BadRequest);

            project.Publish();

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

                _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogPublishSuccess), project.ProjectId);

                // AC-13: Push ProjectListChanged signal tới Mentor
                if (Guid.TryParse(_currentUser.UserId, out var mentorUserIdForSignal))
                {
                    try
                    {
                        await _pushService.PushNewNotificationAsync(mentorUserIdForSignal, new
                        {
                            type      = ProjectSignalConstants.ProjectListChanged,
                            action    = ProjectSignalConstants.Actions.Published,
                            projectId = project.ProjectId
                        }, cancellationToken);
                        _logger.LogInformation(
                            _message.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                            ProjectSignalConstants.Actions.Published, mentorUserIdForSignal, project.ProjectId);
                    }
                    catch (Exception signalEx)
                    {
                        _logger.LogWarning(signalEx, _message.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                            ProjectSignalConstants.Actions.Published, mentorUserIdForSignal, project.ProjectId);
                    }
                }

                return Result<PublishProjectResponse>.Success(new PublishProjectResponse
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
                _logger.LogError(ex, _message.GetMessage(MessageKeys.Projects.LogPublishError), project.ProjectId);
                return Result<PublishProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
