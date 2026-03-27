using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Commands.UpdateProject
{
    public class UpdateProjectHandler : IRequestHandler<UpdateProjectCommand, Result<UpdateProjectResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateProjectHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUser;
        private readonly ICacheService _cacheService;
        private readonly INotificationPushService _pushService;

        public UpdateProjectHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<UpdateProjectHandler> logger,
            IMessageService messageService, ICurrentUserService currentUser, ICacheService cacheService,
            INotificationPushService pushService)
        {
            _unitOfWork     = unitOfWork;
            _mapper         = mapper;
            _logger         = logger;
            _messageService = messageService;
            _currentUser    = currentUser;
            _cacheService   = cacheService;
            _pushService    = pushService;
        }

        public async Task<Result<UpdateProjectResponse>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogUpdating), request.ProjectId, _currentUser.UserId);

            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            var project = await _unitOfWork.Repository<Project>().Query()
                .Include(p => p.InternshipGroup)
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Projects.LogNotFound), request.ProjectId);
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Projects.NotFound), ResultErrorType.NotFound);
            }

            // B9: Scope check — chỉ Mentor tạo project mới được edit (AC-08, AC-14)
            if (project.MentorId != enterpriseUser.EnterpriseUserId)
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            // Block nếu project không còn editable (OperationalStatus không phải Unstarted/Active)
            if (!project.IsEditable)
                return Result<UpdateProjectResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Projects.InvalidStatusForUpdate), ResultErrorType.BadRequest);

            var assignedCount = 0;

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Uniqueness check: ProjectName nếu thay đổi
                if (request.ProjectName is not null && project.ProjectName != request.ProjectName)
                {
                    var nameExists = await _unitOfWork.Repository<Project>()
                        .ExistsAsync(p => p.InternshipId == project.InternshipId
                                       && p.ProjectName == request.ProjectName
                                       && p.ProjectId != request.ProjectId, cancellationToken);
                    if (nameExists)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<UpdateProjectResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.Projects.ProjectNameExistsInternship), ResultErrorType.Conflict);
                    }
                }

                project.Update(
                    request.ProjectName,
                    request.Description,
                    request.StartDate,
                    request.EndDate,
                    request.Field,
                    request.Requirements,
                    request.Deliverables,
                    request.Template);

                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Auto-publish: nếu project còn Draft thì publish sau khi update
                if (project.VisibilityStatus == VisibilityStatus.Draft)
                {
                    project.Publish();
                    await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Notify students nếu project đang assigned cho group
                if (project.InternshipId.HasValue)
                {
                    var studentUserIds = await _unitOfWork.Repository<InternshipStudent>().Query()
                        .Where(s => s.InternshipId == project.InternshipId.Value)
                        .Select(s => s.Student.UserId)
                        .ToListAsync(cancellationToken);

                    foreach (var userId in studentUserIds)
                    {
                        var notif = new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId         = userId,
                            Title          = _messageService.GetMessage(MessageKeys.Projects.NotifUpdatedTitle),
                            Content        = _messageService.GetMessage(MessageKeys.Projects.NotifUpdatedContent, project.ProjectName),
                            Type           = NotificationType.General,
                            ReferenceType  = "Project",
                            ReferenceId    = project.ProjectId
                        };
                        await _unitOfWork.Repository<Notification>().AddAsync(notif, cancellationToken);
                    }

                    if (studentUserIds.Any())
                        await _unitOfWork.SaveChangeAsync(cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Projects.LogUpdateError), request.ProjectId);
                return Result<UpdateProjectResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }

            await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Projects.LogUpdateSuccess), request.ProjectId);

            // AC-13: Push ProjectListChanged signal tới Mentor
            if (Guid.TryParse(_currentUser.UserId, out var mentorUserIdForSignal))
            {
                try
                {
                    await _pushService.PushNewNotificationAsync(mentorUserIdForSignal, new
                    {
                        type      = ProjectSignalConstants.ProjectListChanged,
                        action    = ProjectSignalConstants.Actions.Updated,
                        projectId = project.ProjectId
                    }, cancellationToken);
                    _logger.LogInformation(
                        _messageService.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                        ProjectSignalConstants.Actions.Updated, mentorUserIdForSignal, project.ProjectId);
                }
                catch (Exception signalEx)
                {
                    _logger.LogWarning(signalEx, _messageService.GetMessage(MessageKeys.Projects.LogProjectListChanged),
                        ProjectSignalConstants.Actions.Updated, mentorUserIdForSignal, project.ProjectId);
                }
            }

            // Đếm sinh viên trong group
            if (project.InternshipId.HasValue)
            {
                assignedCount = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .CountAsync(s => s.InternshipId == project.InternshipId.Value, cancellationToken);
            }

            var response = _mapper.Map<UpdateProjectResponse>(project);
            response.AssignedStudentCount = assignedCount;

            return Result<UpdateProjectResponse>.Success(response);
        }
    }
}
