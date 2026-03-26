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

        public MoveStudentsBetweenGroupsHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<MoveStudentsBetweenGroupsHandler> logger,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
            _cacheService = cacheService;
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
                .FirstOrDefaultAsync(g => g.InternshipId == request.FromGroupId, cancellationToken);

            var toGroup = await _unitOfWork.Repository<InternshipGroup>().Query()
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.InternshipId == request.ToGroupId, cancellationToken);

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
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogUpdateError));
                return Result<MoveStudentsBetweenGroupsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}
