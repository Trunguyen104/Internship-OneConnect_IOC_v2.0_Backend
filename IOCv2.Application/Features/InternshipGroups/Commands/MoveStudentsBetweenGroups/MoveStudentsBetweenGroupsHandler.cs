using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.InternshipGroups.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.InternshipGroups.Commands.MoveStudentsBetweenGroups
{
    public class MoveStudentsBetweenGroupsHandler : IRequestHandler<MoveStudentsBetweenGroupsCommand, Result<MoveStudentsBetweenGroupsResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ILogger<MoveStudentsBetweenGroupsHandler> _logger;
        private readonly ICacheService _cacheService;
        private readonly INotificationPushService _pushService;

        public MoveStudentsBetweenGroupsHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<MoveStudentsBetweenGroupsHandler> logger,
            ICacheService cacheService,
            INotificationPushService pushService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
            _cacheService = cacheService;
            _pushService = pushService;
        }

        public async Task<Result<MoveStudentsBetweenGroupsResponse>> Handle(MoveStudentsBetweenGroupsCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                return Result<MoveStudentsBetweenGroupsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.Unauthorized),
                    ResultErrorType.Unauthorized);
            }

            var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(eu => eu.UserId == currentUserId, cancellationToken);

            if (enterpriseUser == null)
            {
                return Result<MoveStudentsBetweenGroupsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseUserNotFound),
                    ResultErrorType.Forbidden);
            }

            if (request.StudentIds == null || !request.StudentIds.Any())
            {
                return Result<MoveStudentsBetweenGroupsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.StudentListRequired),
                    ResultErrorType.BadRequest);
            }

            var fromGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Members)
                .Include(g => g.Mentor)
                .FirstOrDefaultAsync(g => g.InternshipId == request.FromGroupId && g.DeletedAt == null, cancellationToken);

            var toGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Members)
                .Include(g => g.Mentor)
                .FirstOrDefaultAsync(g => g.InternshipId == request.ToGroupId && g.DeletedAt == null, cancellationToken);

            if (fromGroup == null || toGroup == null)
            {
                return Result<MoveStudentsBetweenGroupsResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
            }

            if (fromGroup.EnterpriseId != enterpriseUser.EnterpriseId || toGroup.EnterpriseId != enterpriseUser.EnterpriseId)
            {
                return Result<MoveStudentsBetweenGroupsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.MustBelongToYourEnterprise),
                    ResultErrorType.Forbidden);
            }

            if (fromGroup.PhaseId != toGroup.PhaseId)
            {
                return Result<MoveStudentsBetweenGroupsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.MustBeInSameTerm),
                    ResultErrorType.BadRequest);
            }

            if (fromGroup.Status != GroupStatus.Active || toGroup.Status != GroupStatus.Active)
            {
                return Result<MoveStudentsBetweenGroupsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.MustBeActive),
                    ResultErrorType.BadRequest);
            }

            var membersInFrom = fromGroup.Members.Where(m => request.StudentIds.Contains(m.StudentId)).ToList();
            var distinctRequestedIds = request.StudentIds.Distinct().ToList();
            if (membersInFrom.Count != distinctRequestedIds.Count)
            {
                return Result<MoveStudentsBetweenGroupsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.InternshipGroups.StudentsNotInSourceGroup),
                    ResultErrorType.BadRequest);
            }

            var studentInfos = await _unitOfWork.Repository<Student>().Query()
                .Where(s => distinctRequestedIds.Contains(s.StudentId))
                .Select(s => new { s.StudentId, s.UserId, StudentName = s.User.FullName })
                .ToListAsync(cancellationToken);

            var movedStudentNames = studentInfos.Select(s => s.StudentName).Where(n => !string.IsNullOrWhiteSpace(n)).ToList();

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var memberRepo = _unitOfWork.Repository<InternshipStudent>();
                foreach (var member in membersInFrom)
                {
                    // Hard-delete: xóa hẳn record khỏi DB thay vì soft-delete
                    // để tránh conflict composite PK (InternshipId, StudentId) khi move ngược lại
                    await memberRepo.HardDeleteAsync(member);

                    // Add to ToGroup only if not already in it
                    if (!toGroup.Members.Any(m => m.StudentId == member.StudentId))
                    {
                        toGroup.AddMember(member.StudentId, member.Role);
                    }
                }

                await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(toGroup);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Xóa cache của cả 2 nhóm để đảm bảo dữ liệu cập nhật đúng
                await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(request.FromGroupId), cancellationToken);
                await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(request.ToGroupId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);

                try
                {
                    var notifications = new List<Notification>();

                    foreach (var student in studentInfos)
                    {
                        notifications.Add(new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId = student.UserId,
                            Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationStudentMovedTitle),
                            Content = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationStudentMovedContent, toGroup.GroupName),
                            Type = NotificationType.General,
                            ReferenceType = nameof(InternshipGroup),
                            ReferenceId = toGroup.InternshipId,
                            IsRead = false
                        });
                    }

                    if (fromGroup.MentorId.HasValue && fromGroup.Mentor != null)
                    {
                        notifications.Add(new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId = fromGroup.Mentor.UserId,
                            Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationMentorOldGroupTitle),
                            Content = _messageService.GetMessage(
                                MessageKeys.InternshipGroups.NotificationMentorOldGroupContent,
                                string.Join(", ", movedStudentNames.Any() ? movedStudentNames : distinctRequestedIds.Select(x => x.ToString()))),
                            Type = NotificationType.General,
                            ReferenceType = nameof(InternshipGroup),
                            ReferenceId = fromGroup.InternshipId,
                            IsRead = false
                        });
                    }

                    if (toGroup.MentorId.HasValue && toGroup.Mentor != null)
                    {
                        notifications.Add(new Notification
                        {
                            NotificationId = Guid.NewGuid(),
                            UserId = toGroup.Mentor.UserId,
                            Title = _messageService.GetMessage(MessageKeys.InternshipGroups.NotificationMentorNewGroupTitle),
                            Content = _messageService.GetMessage(
                                MessageKeys.InternshipGroups.NotificationMentorNewGroupContent,
                                string.Join(", ", movedStudentNames.Any() ? movedStudentNames : distinctRequestedIds.Select(x => x.ToString())),
                                toGroup.GroupName),
                            Type = NotificationType.General,
                            ReferenceType = nameof(InternshipGroup),
                            ReferenceId = toGroup.InternshipId,
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
                                referenceId = toGroup.InternshipId,
                                currentUnreadCount = unreadCount
                            }, cancellationToken);
                        }
                    }
                }
                catch (Exception notifyEx)
                {
                    _logger.LogWarning(notifyEx, _messageService.GetMessage(MessageKeys.InternshipGroups.LogMoveNotificationFailed), request.FromGroupId, request.ToGroupId);
                }

                return Result<MoveStudentsBetweenGroupsResponse>.Success(new MoveStudentsBetweenGroupsResponse
                {
                    StudentIds = request.StudentIds,
                    FromGroupId = request.FromGroupId,
                    ToGroupId = request.ToGroupId,
                    Message = _messageService.GetMessage(MessageKeys.InternshipGroups.MoveSuccess)
                });
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogMoveError));
                return Result<MoveStudentsBetweenGroupsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}
