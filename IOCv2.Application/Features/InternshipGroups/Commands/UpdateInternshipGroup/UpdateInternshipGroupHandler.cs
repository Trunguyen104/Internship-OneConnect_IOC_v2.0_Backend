using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Common;
using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Constants;
using AutoMapper;

namespace IOCv2.Application.Features.InternshipGroups.Commands.UpdateInternshipGroup
{
    public class UpdateInternshipGroupHandler : IRequestHandler<UpdateInternshipGroupCommand, Result<UpdateInternshipGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateInternshipGroupHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly INotificationPushService _pushService;

        public UpdateInternshipGroupHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ILogger<UpdateInternshipGroupHandler> logger,
            ICacheService cacheService,
            INotificationPushService pushService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
            _pushService = pushService;
        }

        public async Task<Result<UpdateInternshipGroupResponse>> Handle(UpdateInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogUpdating), request.InternshipId);

            try
            {
                var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId && x.DeletedAt == null, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                    return Result<UpdateInternshipGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                if (entity.Status != GroupStatus.Active)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogGroupNotActive), entity.InternshipId);
                    return Result<UpdateInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.GroupNotActive),
                        ResultErrorType.BadRequest);
                }

                var oldMentorId = entity.MentorId;
                string oldMentorName = string.Empty;

                // Validate PhaseId
                var phaseExists = await _unitOfWork.Repository<InternshipPhase>()
                    .ExistsAsync(p => p.PhaseId == request.PhaseId, cancellationToken);
                if (!phaseExists)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogTermNotFound), request.PhaseId);
                    return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.TermNotFound), ResultErrorType.NotFound);
                }

                // Validate EnterpriseId if provided
                if (request.EnterpriseId.HasValue)
                {
                    var enterpriseExists = await _unitOfWork.Repository<Enterprise>()
                        .ExistsAsync(e => e.EnterpriseId == request.EnterpriseId.Value, cancellationToken);
                    if (!enterpriseExists)
                    {
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogEnterpriseNotFound), request.EnterpriseId);
                        return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseNotFound), ResultErrorType.NotFound);
                    }
                }

                // Keep current mentor unless request explicitly provides a new mentor.
                Guid? resolvedMentorId = entity.MentorId;
                var effectiveEnterpriseId = request.EnterpriseId ?? entity.EnterpriseId;

                // Validate MentorId if provided — request truyền UserId
                if (request.MentorId.HasValue)
                {
                    // Frontend truyền UserId của mentor → tìm ra EnterpriseUserId để lưu DB
                    var mentor = await _unitOfWork.Repository<EnterpriseUser>()
                        .Query()
                        .Include(eu => eu.User)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(eu => eu.UserId == request.MentorId.Value, cancellationToken);

                    if (mentor == null)
                    {
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogMentorNotFound), request.MentorId);
                        return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.MentorNotFound), ResultErrorType.NotFound);
                    }

                    // Chỉ chấp nhận tài khoản có role Mentor
                    if (mentor.User.Role != UserRole.Mentor)
                    {
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogMentorRoleInvalid), request.MentorId);
                        return Result<UpdateInternshipGroupResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.MentorNotFound),
                            ResultErrorType.BadRequest);
                    }

                    if (!effectiveEnterpriseId.HasValue || mentor.EnterpriseId != effectiveEnterpriseId.Value)
                    {
                        _logger.LogWarning(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.LogUnauthorizedEnterpriseAccess),
                            request.InternshipId, request.MentorId, effectiveEnterpriseId);
                        return Result<UpdateInternshipGroupResponse>.Failure(
                            _messageService.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise),
                            ResultErrorType.BadRequest);
                    }

                    resolvedMentorId = mentor.EnterpriseUserId;
                }

                var mentorOwnershipChanged = oldMentorId != resolvedMentorId;
                var projectIdsInGroup = mentorOwnershipChanged
                    ? await _unitOfWork.Repository<Project>().Query()
                        .AsNoTracking()
                        .Where(p => p.InternshipId == entity.InternshipId)
                        .Select(p => p.ProjectId)
                        .ToListAsync(cancellationToken)
                    : new List<Guid>();

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                entity.UpdateInfo(
                    request.GroupName,
                    request.Description,
                    request.PhaseId,
                    request.EnterpriseId,
                    resolvedMentorId, // EnterpriseUserId
                    request.StartDate,
                    request.EndDate
                );

                await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(entity);

                if (mentorOwnershipChanged)
                {
                    await _unitOfWork.Repository<Project>().ExecuteUpdateAsync(
                        p => p.InternshipId == entity.InternshipId,
                        s => s.SetProperty(p => p.MentorId, resolvedMentorId)
                              .SetProperty(p => p.UpdatedAt, DateTime.UtcNow),
                        cancellationToken);
                }

                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved > 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(entity.InternshipId), cancellationToken);
                    await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);

                    if (mentorOwnershipChanged)
                    {
                        await _cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);
                        foreach (var projectId in projectIdsInGroup)
                            await _cacheService.RemoveAsync(ProjectCacheKeys.Project(projectId), cancellationToken);

                        try
                        {
                            var mentorIds = new[] { oldMentorId, resolvedMentorId }
                                .Where(x => x.HasValue)
                                .Select(x => x!.Value)
                                .Distinct()
                                .ToList();

                            var mentorUsers = mentorIds.Any()
                                ? await _unitOfWork.Repository<EnterpriseUser>().Query()
                                    .Include(eu => eu.User)
                                    .Where(eu => mentorIds.Contains(eu.EnterpriseUserId))
                                    .ToListAsync(cancellationToken)
                                : new List<EnterpriseUser>();

                            var oldMentor = oldMentorId.HasValue
                                ? mentorUsers.FirstOrDefault(x => x.EnterpriseUserId == oldMentorId.Value)
                                : null;
                            var newMentor = resolvedMentorId.HasValue
                                ? mentorUsers.FirstOrDefault(x => x.EnterpriseUserId == resolvedMentorId.Value)
                                : null;

                            oldMentorName = oldMentor != null ? oldMentor.User.FullName : oldMentorName;

                            var notifications = new List<Notification>();
                            if (oldMentor != null)
                            {
                                notifications.Add(new Notification
                                {
                                    NotificationId = Guid.NewGuid(),
                                    UserId = oldMentor.UserId,
                                    Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationMentorReplacedOldTitle),
                                    Content = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationMentorReplacedOldContent, entity.GroupName),
                                    Type = NotificationType.General,
                                    ReferenceType = nameof(InternshipGroup),
                                    ReferenceId = entity.InternshipId,
                                    IsRead = false
                                });
                            }

                            if (newMentor != null)
                            {
                                notifications.Add(new Notification
                                {
                                    NotificationId = Guid.NewGuid(),
                                    UserId = newMentor.UserId,
                                    Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationMentorReplacedNewTitle),
                                    Content = _messageService.GetMessage(
                                        MessageKeys.InternshipGroups.NotificationMentorReplacedNewContent,
                                        entity.GroupName,
                                        string.IsNullOrWhiteSpace(oldMentorName) ? _messageService.GetMessage(MessageKeys.InternshipGroups.Unassigned) : oldMentorName),
                                    Type = NotificationType.General,
                                    ReferenceType = nameof(InternshipGroup),
                                    ReferenceId = entity.InternshipId,
                                    IsRead = false
                                });
                            }

                            foreach (var notification in notifications)
                                await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);

                            if (notifications.Any())
                            {
                                await _unitOfWork.SaveChangeAsync(cancellationToken);

                                foreach (var recipientUserId in notifications.Select(n => n.UserId).Distinct())
                                {
                                    var unreadCount = await _unitOfWork.Repository<Notification>()
                                        .CountAsync(n => n.UserId == recipientUserId && !n.IsRead, cancellationToken);

                                    await _pushService.PushNewNotificationAsync(recipientUserId, new
                                    {
                                        type = NotificationType.General,
                                        referenceType = nameof(InternshipGroup),
                                        referenceId = entity.InternshipId,
                                        currentUnreadCount = unreadCount
                                    }, cancellationToken);
                                }
                            }
                        }
                        catch (Exception notifyEx)
                        {
                            _logger.LogWarning(notifyEx, _messageService.GetMessage(MessageKeys.InternshipGroups.LogMentorSwapNotificationFailed), entity.InternshipId);
                        }
                    }

                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogUpdatedSuccess), entity.InternshipId);

                    var response = _mapper.Map<UpdateInternshipGroupResponse>(entity);
                    return Result<UpdateInternshipGroupResponse>.Success(response);
                }

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(_messageService.GetMessage(MessageKeys.InternshipGroups.LogUpdateFailed));
                return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogUpdateError));
                return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
