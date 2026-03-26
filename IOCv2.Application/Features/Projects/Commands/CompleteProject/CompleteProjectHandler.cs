using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Commands.CompleteProject
{
    public class CompleteProjectHandler : IRequestHandler<CompleteProjectCommand, Result<CompleteProjectResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IMessageService _message;
        private readonly ICacheService _cacheService;
        private readonly ILogger<CompleteProjectHandler> _logger;

        public CompleteProjectHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUser,
            IMessageService message,
            ICacheService cacheService,
            ILogger<CompleteProjectHandler> logger)
        {
            _unitOfWork   = unitOfWork;
            _currentUser  = currentUser;
            _message      = message;
            _cacheService = cacheService;
            _logger       = logger;
        }

        public async Task<Result<CompleteProjectResponse>> Handle(CompleteProjectCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<CompleteProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<CompleteProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            var project = await _unitOfWork.Repository<Project>().Query()
                .Include(p => p.InternshipGroup)
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
                return Result<CompleteProjectResponse>.NotFound(_message.GetMessage(MessageKeys.Projects.NotFound));

            // Scope check
            if (project.MentorId != enterpriseUser.EnterpriseUserId &&
                project.InternshipGroup?.MentorId != enterpriseUser.EnterpriseUserId)
                return Result<CompleteProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            if (project.Status != ProjectStatus.Published)
                return Result<CompleteProjectResponse>.Failure(_message.GetMessage(MessageKeys.Projects.InvalidStatusForComplete), ResultErrorType.BadRequest);

            // Warning-only count: số sinh viên đang thuộc group của project.
            var pendingCount = 0;
            if (project.InternshipId.HasValue)
            {
                pendingCount = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .CountAsync(s => s.InternshipId == project.InternshipId.Value, cancellationToken);
            }

            project.SetStatus(ProjectStatus.Completed);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

                _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogCompleteSuccess), project.ProjectId);

                return Result<CompleteProjectResponse>.Success(new CompleteProjectResponse
                {
                    ProjectId            = project.ProjectId,
                    Status               = ProjectStatus.Completed,
                    PendingStudentsCount = pendingCount,
                    UpdatedAt            = project.UpdatedAt ?? DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _message.GetMessage(MessageKeys.Projects.LogCompleteError), project.ProjectId);
                return Result<CompleteProjectResponse>.Failure(_message.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
