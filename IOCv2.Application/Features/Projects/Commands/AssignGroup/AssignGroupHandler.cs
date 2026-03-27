using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Commands.AssignGroup
{
    public class AssignGroupHandler : IRequestHandler<AssignGroupCommand, Result<AssignGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUser;
        private readonly IMessageService _message;
        private readonly ICacheService _cacheService;
        private readonly ILogger<AssignGroupHandler> _logger;
        private readonly INotificationPushService _pushService;

        public AssignGroupHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser,
            IMessageService message, ICacheService cacheService,
            ILogger<AssignGroupHandler> logger, INotificationPushService pushService)
        {
            _unitOfWork   = unitOfWork;
            _currentUser  = currentUser;
            _message      = message;
            _cacheService = cacheService;
            _logger       = logger;
            _pushService  = pushService;
        }

        public async Task<Result<AssignGroupResponse>> Handle(AssignGroupCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUser.UserId, out var currentUserId))
                return Result<AssignGroupResponse>.Failure(_message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<AssignGroupResponse>.Failure(_message.GetMessage(MessageKeys.Projects.MentorNotFound), ResultErrorType.Forbidden);

            var project = await _unitOfWork.Repository<Project>().Query()
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
                return Result<AssignGroupResponse>.NotFound(_message.GetMessage(MessageKeys.Projects.NotFound));

            // Scope: chỉ Mentor tạo project
            if (project.MentorId != enterpriseUser.EnterpriseUserId)
                return Result<AssignGroupResponse>.Failure(_message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            // Project phải là Unstarted
            if (project.OperationalStatus != OperationalStatus.Unstarted)
                return Result<AssignGroupResponse>.Failure("Project is already assigned to a group.", ResultErrorType.BadRequest);

            // Load group
            var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Enterprise)
                .FirstOrDefaultAsync(g => g.InternshipId == request.InternshipId, cancellationToken);

            if (group == null)
                return Result<AssignGroupResponse>.NotFound(_message.GetMessage(MessageKeys.Internships.NotFound));

            if (group.Status == GroupStatus.Archived || group.Status == GroupStatus.Finished)
                return Result<AssignGroupResponse>.Failure("Group is not active.", ResultErrorType.BadRequest);

            if (group.EndDate.HasValue && group.EndDate.Value.Date < DateTime.UtcNow.Date)
                return Result<AssignGroupResponse>.Failure("Group's internship phase has ended.", ResultErrorType.BadRequest);

            // Mentor phải phụ trách group
            if (group.MentorId != enterpriseUser.EnterpriseUserId)
                return Result<AssignGroupResponse>.Failure(_message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

            project.AssignToGroup(group.InternshipId, group.StartDate, group.EndDate);

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error assigning group to project {ProjectId}", project.ProjectId);
                return Result<AssignGroupResponse>.Failure(_message.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }

            // Notify students nếu project đã Published
            if (project.VisibilityStatus == VisibilityStatus.Published)
            {
                try
                {
                    var studentUserIds = await _unitOfWork.Repository<InternshipStudent>().Query()
                        .Where(s => s.InternshipId == group.InternshipId)
                        .Select(s => s.Student.UserId)
                        .ToListAsync(cancellationToken);

                    foreach (var userId in studentUserIds)
                    {
                        var notif = new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId         = userId,
                            Title          = "Dự án mới trong nhóm",
                            Content        = $"Dự án {project.ProjectName} vừa được thêm vào nhóm của bạn.",
                            Type           = NotificationType.General,
                            ReferenceType  = "Project",
                            ReferenceId    = project.ProjectId
                        };
                        await _unitOfWork.Repository<Notification>().AddAsync(notif, cancellationToken);
                    }

                    if (studentUserIds.Any())
                    {
                        await _unitOfWork.SaveChangeAsync(cancellationToken);
                        foreach (var userId in studentUserIds)
                        {
                            var unreadCount = await _unitOfWork.Repository<Notification>()
                                .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
                            await _pushService.PushNewNotificationAsync(userId, new
                            {
                                type             = NotificationType.General,
                                referenceType    = "Project",
                                referenceId      = project.ProjectId,
                                currentUnreadCount = unreadCount
                            }, cancellationToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send notifications for project {ProjectId} assignment", project.ProjectId);
                }
            }

            await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
            await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);

            _logger.LogInformation("Project {ProjectId} assigned to group {InternshipId}", project.ProjectId, group.InternshipId);

            return Result<AssignGroupResponse>.Success(new AssignGroupResponse
            {
                ProjectId         = project.ProjectId,
                InternshipId      = group.InternshipId,
                VisibilityStatus  = project.VisibilityStatus,
                OperationalStatus = project.OperationalStatus,
                StartDate         = project.StartDate,
                EndDate           = project.EndDate,
                UpdatedAt         = project.UpdatedAt ?? DateTime.UtcNow
            });
        }
    }
}
