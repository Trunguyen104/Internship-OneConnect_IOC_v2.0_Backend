using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Commands.DeleteProject
{
    public class DeleteProjectHandler : IRequestHandler<DeleteProjectCommand, Result<string>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeleteProjectHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUser;
        private readonly ICacheService _cacheService;

        public DeleteProjectHandler(IUnitOfWork unitOfWork, ILogger<DeleteProjectHandler> logger,
            IMessageService messageService, ICurrentUserService currentUser, ICacheService cacheService)
        {
            _unitOfWork     = unitOfWork;
            _logger         = logger;
            _messageService = messageService;
            _currentUser    = currentUser;
            _cacheService   = cacheService;
        }

        public async Task<Result<string>> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogDeleting), request.ProjectId, _currentUser.UserId);

            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            var project = await _unitOfWork.Repository<Project>().Query()
                .Include(p => p.InternshipGroup)
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Projects.LogNotFound), request.ProjectId);
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Projects.NotFound), ResultErrorType.NotFound);
            }

            // Scope check
            if (project.MentorId != enterpriseUser.EnterpriseUserId &&
                project.InternshipGroup?.MentorId != enterpriseUser.EnterpriseUserId)
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            var hasWorkItems = await _unitOfWork.Repository<WorkItem>().Query()
                .AnyAsync(w => w.ProjectId == project.ProjectId, cancellationToken);
            if (hasWorkItems)
                return Result<string>.Failure(
                    _messageService.GetMessage(MessageKeys.Projects.CannotDeleteHasData),
                    ResultErrorType.BadRequest);

            var hasSprints = await _unitOfWork.Repository<Sprint>().Query()
                .AnyAsync(s => s.ProjectId == project.ProjectId, cancellationToken);
            if (hasSprints)
                return Result<string>.Failure(
                    _messageService.GetMessage(MessageKeys.Projects.CannotDeleteHasData),
                    ResultErrorType.BadRequest);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                string logKey;
                if (project.Status == ProjectStatus.Draft)
                {
                    // Draft project without runtime data can be hard-deleted.
                    await _unitOfWork.Repository<Project>().HardDeleteAsync(project, cancellationToken);
                    logKey = MessageKeys.Projects.LogDeleteHard;
                }
                else
                {
                    // Non-draft project is soft-deleted for audit history.
                    project.DeletedAt = DateTime.UtcNow;
                    await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                    logKey = MessageKeys.Projects.LogDeleteSoft;
                }

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

                _logger.LogInformation(_messageService.GetMessage(logKey), request.ProjectId);
                return Result<string>.Success(_messageService.GetMessage(MessageKeys.Projects.DeleteSuccess));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Projects.LogDeleteError), request.ProjectId);
                return Result<string>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
