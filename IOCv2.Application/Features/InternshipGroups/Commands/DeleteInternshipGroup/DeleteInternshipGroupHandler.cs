using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Common;
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
            try
            {
                var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(g => g.Members)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                    return Result<DeleteInternshipGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                // ── 2. Chỉ nhóm Active mới được phép xóa (AC-G09) ────────────────
                if (entity.Status != GroupStatus.Active)
                {
                    _logger.LogWarning("Attempted to delete group {InternshipId} with status {Status}.", request.InternshipId, entity.Status);
                // Enterprise ownership check
                if (entity.EnterpriseId != enterpriseUser.EnterpriseId)
                    return Result<DeleteInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.GroupNotActive),
                        ResultErrorType.BadRequest);
                }
                        _messageService.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise),
                        ResultErrorType.Forbidden);

                // ── 3. Kiểm tra "data thực tế" (AC-G09) ──────────────────────────
                // Data thực tế = logbook entries + vi phạm + project có WorkItem
                // KHÔNG tính: SV trong nhóm, project chưa có nội dung
                var hasActivityData = HasRealActivityData(entity);

                if (hasActivityData)
                // AC-13: Group có thể bị xóa kể cả khi còn sinh viên.
                // Khi xóa → projects bị orphan-ize, mentor được thông báo.

                if (entity.Status == GroupStatus.Archived || entity.Status == GroupStatus.Finished)
                {
                    _logger.LogWarning(
                        "Cannot delete group {InternshipId}: group has actual activity data (logbooks={L}, violations={V}, projectsWithItems={P}).",
                        request.InternshipId,
                        entity.Logbooks.Count,
                        entity.ViolationReports.Count,
                        entity.Projects.Count(p => p.WorkItems.Any()));

                    return Result<DeleteInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.CannotDeleteNotActive),
                        _messageService.GetMessage(MessageKeys.InternshipGroups.HasActivityData),
                        ResultErrorType.BadRequest);
                }

                // ── 4. Không có data thực tế → tiến hành xóa ─────────────────────
                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    projectsToOrphan = await _unitOfWork.Repository<Project>().Query()
                        .Where(p => p.InternshipId == request.InternshipId
                                 && (p.Status == ProjectStatus.Draft || p.Status == ProjectStatus.Published))
                        .ToListAsync(cancellationToken);

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

                // a) Auto-unlink tất cả SV (InternshipStudent records)
                //    EF cascade sẽ tự xóa các bản ghi InternshipStudent khi nhóm bị xóa,
                //    nhưng log rõ để trace.
                if (entity.Members.Any())
                {
                    _logger.LogInformation(
                        "Auto-unlinking {Count} student(s) from group {InternshipId} before deletion.",
                        entity.Members.Count, request.InternshipId);
                    // EF cascade delete sẽ xử lý, trạng thái Placed của StudentTerm KHÔNG thay đổi theo AC
                }

                // b) Unlink projects không có nội dung: project sẽ bị cascade-deleted cùng nhóm.
                //    (Project.InternshipId là required FK nên không thể null ra; chúng sẽ bị xóa.)
                if (entity.Projects.Any(p => !p.WorkItems.Any()))
                {
                    _logger.LogInformation(
                        "Group {InternshipId} has {Count} project(s) without content — will be removed with the group.",
                        request.InternshipId,
                        entity.Projects.Count(p => !p.WorkItems.Any()));
                }

                // c) Hard delete nhóm — cascade xóa Members, Projects (không có content), Stakeholders, etc.
                await _unitOfWork.Repository<InternshipGroup>().DeleteAsync(entity);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved > 0)
                {
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

                if (projectsToOrphan.Any() && entity.MentorId.HasValue && entity.Mentor != null)
                {
                    var mentorUserId = entity.Mentor.UserId;

                    var notification = new Notification
                    {
                        NotificationId = Guid.NewGuid(),
                        UserId = mentorUserId,
                        Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationOrphanTitle),
                        Content = string.Format(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationOrphanContent),
                            entity.GroupName,
                            projectsToOrphan.Count),
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
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleteError), request.InternshipId);
                return Result<DeleteInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }

        /// <summary>
        /// Kiểm tra xem nhóm có "data thực tế" hay không.
        /// Data thực tế = logbook entries, vi phạm, hoặc ít nhất 1 project đã có WorkItems.
        /// KHÔNG tính: SV trong nhóm, project chỉ được link nhưng chưa có nội dung.
        /// </summary>
        private static bool HasRealActivityData(InternshipGroup group)
        {
            // 1. Có bất kỳ logbook entry nào
            if (group.Logbooks.Any())
                return true;

            // 2. Có bất kỳ báo cáo vi phạm nào
            if (group.ViolationReports.Any())
                return true;

            // 3. Có bất kỳ project nào đã có WorkItems (task/submission/logbook)
            if (group.Projects.Any(p => p.WorkItems.Any()))
                return true;

            return false;
        }
    }
}
