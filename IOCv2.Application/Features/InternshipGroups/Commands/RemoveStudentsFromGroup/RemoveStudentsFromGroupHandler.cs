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

namespace IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup
{
    public class RemoveStudentsFromGroupHandler : IRequestHandler<RemoveStudentsFromGroupCommand, Result<RemoveStudentsFromGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<RemoveStudentsFromGroupHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly INotificationPushService _pushService;

        public RemoveStudentsFromGroupHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            IMapper mapper,
            ILogger<RemoveStudentsFromGroupHandler> logger,
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

        public async Task<Result<RemoveStudentsFromGroupResponse>> Handle(RemoveStudentsFromGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogRemovingStudents), request.InternshipId);

            // Enterprise ownership check
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                return Result<RemoveStudentsFromGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
                return Result<RemoveStudentsFromGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);

            try
            {
                var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(g => g.Members)
                        .ThenInclude(m => m.Student)
                            .ThenInclude(s => s.User)
                    .Include(g => g.Mentor)
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                if (group == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                    return Result<RemoveStudentsFromGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                if (group.Status != GroupStatus.Active)
                {
                    return Result<RemoveStudentsFromGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.GroupNotActive),
                        ResultErrorType.BadRequest);
                }

                if (group.EnterpriseId != enterpriseUser.EnterpriseId)
                    return Result<RemoveStudentsFromGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise),
                        ResultErrorType.Forbidden);

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var membersToRemove = group.Members
                    .Where(m => request.StudentIds.Contains(m.StudentId))
                    .ToList();

                if (!membersToRemove.Any())
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogRemoveStudentsFailed));
                    return Result<RemoveStudentsFromGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.StudentListToRemoveRequired),
                        ResultErrorType.BadRequest);
                }

                var memberRepo = _unitOfWork.Repository<InternshipStudent>();
                foreach (var member in membersToRemove)
                {
                    // Hard-delete: xóa hẳn record để sinh viên có thể được thêm vào nhóm khác
                    // Soft-delete sẽ gây conflict composite PK (InternshipId, StudentId) khi thêm lại
                    await memberRepo.HardDeleteAsync(member);
                }

                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved > 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    if (group.MentorId.HasValue && group.Mentor != null && membersToRemove.Any())
                    {
                        var mentorUserId = group.Mentor.UserId;
                        var removedStudentNames = membersToRemove
                            .Select(m => string.IsNullOrWhiteSpace(m.Student.User.FullName) ? m.StudentId.ToString() : m.Student.User.FullName)
                            .Distinct()
                            .ToList();

                        var mentorNotification = new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId = mentorUserId,
                            Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationStudentRemovedMentorTitle),
                            Content = _messageService.GetMessage(
                                MessageKeys.InternshipGroups.NotificationStudentRemovedMentorContent,
                                string.Join(", ", removedStudentNames.Any() ? removedStudentNames : membersToRemove.Select(m => m.StudentId.ToString())),
                                group.GroupName),
                            Type = NotificationType.General,
                            ReferenceType = nameof(InternshipGroup),
                            ReferenceId = group.InternshipId,
                            IsRead = false
                        };

                        await _unitOfWork.Repository<Notification>().AddAsync(mentorNotification, cancellationToken);

                        foreach (var member in membersToRemove)
                        {
                            var studentUserId = member.Student.UserId;
                            if (studentUserId == Guid.Empty)
                            {
                                continue;
                            }

                            var studentNotification = new Notification
                            {
                                NotificationId = Guid.NewGuid(),
                                UserId = studentUserId,
                                Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationStudentRemovedStudentTitle),
                                Content = _messageService.GetMessage(
                                    MessageKeys.InternshipGroups.NotificationStudentRemovedStudentContent,
                                    group.GroupName),
                                Type = NotificationType.General,
                                ReferenceType = nameof(InternshipGroup),
                                ReferenceId = group.InternshipId,
                                IsRead = false
                            };
                            await _unitOfWork.Repository<Notification>().AddAsync(studentNotification, cancellationToken);
                        }

                        await _unitOfWork.SaveChangeAsync(cancellationToken);

                        var unreadCount = await _unitOfWork.Repository<Notification>()
                            .CountAsync(n => n.UserId == mentorUserId && !n.IsRead, cancellationToken);

                        await _pushService.PushNewNotificationAsync(mentorUserId, new
                        {
                            type = NotificationType.General,
                            referenceType = nameof(InternshipGroup),
                            referenceId = group.InternshipId,
                            currentUnreadCount = unreadCount
                        }, cancellationToken);

                        foreach (var member in membersToRemove)
                        {
                            var studentUserId = member.Student.UserId;
                            if (studentUserId == Guid.Empty)
                            {
                                continue;
                            }

                            var studentUnreadCount = await _unitOfWork.Repository<Notification>()
                                .CountAsync(n => n.UserId == studentUserId && !n.IsRead, cancellationToken);

                            await _pushService.PushNewNotificationAsync(studentUserId, new
                            {
                                type = NotificationType.General,
                                referenceType = nameof(InternshipGroup),
                                referenceId = group.InternshipId,
                                currentUnreadCount = studentUnreadCount
                            }, cancellationToken);
                        }
                    }

                    await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(group.InternshipId), cancellationToken);
                    await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogRemovedStudentsSuccess), request.InternshipId);

                    var updatedGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                        .Include(g => g.Members)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                    var response = _mapper.Map<RemoveStudentsFromGroupResponse>(updatedGroup);
                    return Result<RemoveStudentsFromGroupResponse>.Success(response);
                }

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(_messageService.GetMessage(MessageKeys.InternshipGroups.LogRemoveStudentsFailed));
                return Result<RemoveStudentsFromGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogRemoveStudentsError));
                return Result<RemoveStudentsFromGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
