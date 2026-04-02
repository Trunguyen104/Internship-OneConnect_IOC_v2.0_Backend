using AutoMapper;
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

namespace IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup
{
    public class DeleteInternshipGroupHandler : IRequestHandler<DeleteInternshipGroupCommand, Result<DeleteInternshipGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<DeleteInternshipGroupHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly INotificationPushService _pushService;

        public DeleteInternshipGroupHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IMapper mapper,
            ILogger<DeleteInternshipGroupHandler> logger,
            ICacheService cacheService,
            INotificationPushService pushService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
            _pushService = pushService;
        }

        public async Task<Result<DeleteInternshipGroupResponse>> Handle(DeleteInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleting), request.InternshipId);

            // Scope check: chỉ enterprise user thuộc cùng enterprise mới được xóa group
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<DeleteInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<DeleteInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);

            List<Project> projectsToOrphan = new();
            List<Guid> affectedStudentUserIds = new();
            List<string> publishedProjectNames = new();
            try
            {
                var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(g => g.Members)
                    .Include(g => g.Mentor)
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                    return Result<DeleteInternshipGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                // Enterprise ownership check
                if (entity.EnterpriseId != enterpriseUser.EnterpriseId)
                    return Result<DeleteInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise),
                        ResultErrorType.Forbidden);

                affectedStudentUserIds = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .Where(m => m.InternshipId == entity.InternshipId)
                    .Select(m => m.Student.UserId)
                    .ToListAsync(cancellationToken);

                // AC-13: Group có thể bị xóa kể cả khi còn sinh viên.
                // Khi xóa → projects bị orphan-ize, mentor được thông báo.
                if (entity.Status == GroupStatus.Archived || entity.Status == GroupStatus.Finished)
                {
                    return Result<DeleteInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.CannotDeleteNotActive),
                        ResultErrorType.BadRequest);
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    // B13: Dùng OperationalStatus thay vì legacy Status (luôn null với project mới)
                    projectsToOrphan = await _unitOfWork.Repository<Project>().Query()
                        .Where(p => p.InternshipId == request.InternshipId
                                 && p.OperationalStatus == OperationalStatus.Active)
                        .ToListAsync(cancellationToken);

                    publishedProjectNames = projectsToOrphan
                        .Where(p => p.VisibilityStatus == VisibilityStatus.Published)
                        .Select(p => p.ProjectName)
                        .Distinct()
                        .ToList();

                    foreach (var project in projectsToOrphan)
                    {
                        project.SetOrphan();
                        await _unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                    }

                    if (projectsToOrphan.Any())
                    {
                        _logger.LogInformation(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.LogOrphanizeProjects),
                            projectsToOrphan.Count,
                            request.InternshipId);
                    }

                    await _unitOfWork.Repository<InternshipGroup>().DeleteAsync(entity);
                    await _unitOfWork.SaveChangeAsync(cancellationToken);
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleteError), request.InternshipId);
                    return Result<DeleteInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.InternalError),
                        ResultErrorType.InternalServerError);
                }

                // Post-commit: gửi thông báo cho mentor nếu có project bị orphan.
                // Lỗi thông báo không được làm rollback kết quả xóa group đã commit.
                if (projectsToOrphan.Any() && entity.MentorId.HasValue && entity.Mentor != null)
                {
                    try
                    {
                        var mentorUserId = entity.Mentor.UserId;

                        var notification = new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId = mentorUserId,
                            Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationOrphanTitle),
                            Content = string.Format(
                                _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationOrphanContent),
                                entity.GroupName),
                            Type = NotificationType.General,
                            ReferenceType = nameof(InternshipGroup),
                            ReferenceId = entity.InternshipId,
                            IsRead = false
                        };

                        await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
                        await _unitOfWork.SaveChangeAsync(cancellationToken);

                        var unreadCount = await _unitOfWork.Repository<Notification>()
                            .CountAsync(n => n.UserId == mentorUserId && !n.IsRead, cancellationToken);

                        await _pushService.PushNewNotificationAsync(mentorUserId, new
                        {
                            type = NotificationType.General,
                            referenceType = nameof(InternshipGroup),
                            referenceId = entity.InternshipId,
                            currentUnreadCount = unreadCount
                        }, cancellationToken);
                    }
                    catch (Exception notifyEx)
                    {
                        _logger.LogWarning(notifyEx,
                            _messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleteNotificationFailed),
                            request.InternshipId);
                    }
                }

                if (publishedProjectNames.Any() && affectedStudentUserIds.Any())
                {
                    try
                    {
                        foreach (var userId in affectedStudentUserIds.Distinct())
                        {
                            foreach (var projectName in publishedProjectNames)
                            {
                                var notification = new Notification
                                {
                                    NotificationId = Guid.NewGuid(),
                                    UserId = userId,
                                    Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationGroupDeletedStudentTitle),
                                    Content = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationGroupDeletedStudentContent, projectName),
                                    Type = NotificationType.General,
                                    ReferenceType = nameof(InternshipGroup),
                                    ReferenceId = entity.InternshipId,
                                    IsRead = false
                                };
                                await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
                            }
                        }

                        await _unitOfWork.SaveChangeAsync(cancellationToken);

                        foreach (var userId in affectedStudentUserIds.Distinct())
                        {
                            var unreadCount = await _unitOfWork.Repository<Notification>()
                                .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

                            await _pushService.PushNewNotificationAsync(userId, new
                            {
                                type = NotificationType.General,
                                referenceType = nameof(InternshipGroup),
                                referenceId = entity.InternshipId,
                                currentUnreadCount = unreadCount
                            }, cancellationToken);
                        }
                    }
                    catch (Exception notifyStudentsEx)
                    {
                        _logger.LogWarning(notifyStudentsEx,
                            _messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleteStudentNotificationFailed),
                            request.InternshipId);
                    }
                }

                await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(request.InternshipId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);
                await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);
                foreach (var project in projectsToOrphan)
                {
                    await _cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);
                }

                _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDeletedSuccess), request.InternshipId);
                var response = _mapper.Map<DeleteInternshipGroupResponse>(entity);
                return Result<DeleteInternshipGroupResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleteError), request.InternshipId);
                return Result<DeleteInternshipGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}
