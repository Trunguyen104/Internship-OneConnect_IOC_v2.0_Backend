using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
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

        public MoveStudentsBetweenGroupsHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IMessageService messageService,
            ILogger<MoveStudentsBetweenGroupsHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _logger = logger;
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

            if (fromGroup.TermId != toGroup.TermId)
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
                    // Remove from FromGroup
                    await memberRepo.DeleteAsync(member);

                    // Add to ToGroup only if not already in it
                    if (!toGroup.Members.Any(m => m.StudentId == member.StudentId))
                    {
                        toGroup.AddMember(member.StudentId, member.Role);
                    }
                }

                await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(toGroup);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

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
                _logger.LogError(ex, MessageKeys.InternshipGroups.LogUpdateError);
                return Result<MoveStudentsBetweenGroupsResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}
