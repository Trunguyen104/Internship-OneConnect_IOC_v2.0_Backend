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
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<RemoveStudentsFromGroupHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly INotificationPushService _pushService;

        public RemoveStudentsFromGroupHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ILogger<RemoveStudentsFromGroupHandler> logger,
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

        public async Task<Result<RemoveStudentsFromGroupResponse>> Handle(RemoveStudentsFromGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogRemovingStudents), request.InternshipId);

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

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var membersToRemove = group.Members
                    .Where(m => request.StudentIds.Contains(m.StudentId))
                    .ToList();

                var memberRepo = _unitOfWork.Repository<InternshipStudent>();
                foreach (var member in membersToRemove)
                {
                    // Hard-delete: xóa hẳn record để sinh viên có thể được thêm vào nhóm khác
                    // Soft-delete sẽ gây conflict composite PK (InternshipId, StudentId) khi thêm lại
                    await memberRepo.HardDeleteAsync(member);
                }

                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved >= 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);

                    if (group.MentorId.HasValue && group.Mentor != null && membersToRemove.Any())
                    {
                        var mentorUserId = group.Mentor.UserId;

                        foreach (var member in membersToRemove)
                        {
                            var studentName = member.Student?.User?.FullName ?? member.StudentId.ToString();

                            var notification = new Notification
                            {
                                NotificationId = Guid.NewGuid(),
                                UserId = mentorUserId,
                                Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationProjectCleanupTitle),
                                Content = string.Format(
                                    _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationProjectCleanupContent),
                                    studentName),
                                Type = NotificationType.General,
                                ReferenceType = nameof(InternshipGroup),
                                ReferenceId = group.InternshipId,
                                IsRead = false
                            };

                            await _unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
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
