using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
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

        public RemoveStudentsFromGroupHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ILogger<RemoveStudentsFromGroupHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<RemoveStudentsFromGroupResponse>> Handle(RemoveStudentsFromGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogRemovingStudents), request.InternshipId);

            try
            {
                // Không dùng AsNoTracking — cần EF track để xóa InternshipStudent
                var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                if (group == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                    return Result<RemoveStudentsFromGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Xóa trực tiếp các InternshipStudent record thay vì UpdateAsync group
                var membersToRemove = group.Members
                    .Where(m => request.StudentIds.Contains(m.StudentId))
                    .ToList();

                var memberRepo = _unitOfWork.Repository<InternshipStudent>();
                foreach (var member in membersToRemove)
                {
                    await memberRepo.DeleteAsync(member);
                }

                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved >= 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    _logger.LogInformation(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.LogRemovedStudentsSuccess),
                        request.InternshipId);

                    // Reload group sau khi xóa để response đúng
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
                return Result<RemoveStudentsFromGroupResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }
    }
}
