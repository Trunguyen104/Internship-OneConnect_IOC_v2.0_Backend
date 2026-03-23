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
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<DeleteInternshipGroupHandler> _logger;
        private readonly ICacheService _cacheService;

        public DeleteInternshipGroupHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ILogger<DeleteInternshipGroupHandler> logger,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<DeleteInternshipGroupResponse>> Handle(DeleteInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleting), request.InternshipId);

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

                // Chặn xóa nếu nhóm còn sinh viên
                if (entity.Members.Any())
                {
                    _logger.LogWarning("Attempted to delete group {InternshipId} which still has {Count} student(s).",
                        request.InternshipId, entity.Members.Count);
                    return Result<DeleteInternshipGroupResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.InternshipGroups.HasStudents),
                        ResultErrorType.BadRequest);
                }

                if (entity.Status != GroupStatus.Active)
                {
                    _logger.LogWarning("Attempted to delete group {InternshipId} which is not Active.", request.InternshipId);
                    return Result<DeleteInternshipGroupResponse>.Failure(
                        "Nhóm đã kết thúc hoặc lưu trữ, không thể xóa.",
                        ResultErrorType.BadRequest);
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                await _unitOfWork.Repository<InternshipGroup>().DeleteAsync(entity);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved > 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(request.InternshipId), cancellationToken);
                    await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDeletedSuccess), request.InternshipId);

                    var response = _mapper.Map<DeleteInternshipGroupResponse>(entity);
                    return Result<DeleteInternshipGroupResponse>.Success(response);
                }

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleteFailed));
                return Result<DeleteInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleteError), request.InternshipId);
                return Result<DeleteInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
