using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.InternshipGroups.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Constants;
using AutoMapper;

namespace IOCv2.Application.Features.InternshipGroups.Commands.RemoveStudentsFromGroup
{
    public class RemoveStudentsFromGroupHandler : IRequestHandler<RemoveStudentsFromGroupCommand, Result<RemoveStudentsFromGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<RemoveStudentsFromGroupHandler> _logger;
        private readonly ICacheService _cacheService;

        public RemoveStudentsFromGroupHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ILogger<RemoveStudentsFromGroupHandler> logger,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
            _cacheService = cacheService;
        }

        public async Task<Result<RemoveStudentsFromGroupResponse>> Handle(RemoveStudentsFromGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogRemovingStudents), request.InternshipId);

            try
            {
                var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(g => g.Members)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                if (group == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                    return Result<RemoveStudentsFromGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                foreach (var sId in request.StudentIds)
                {
                    group.RemoveMember(sId);
                }

                await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(group);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved >= 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    await _cacheService.RemoveAsync(InternshipGroupCacheKeys.Group(group.InternshipId), cancellationToken);
                    await _cacheService.RemoveByPatternAsync(InternshipGroupCacheKeys.GroupListPattern(), cancellationToken);
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogRemovedStudentsSuccess), request.InternshipId);

                    var response = _mapper.Map<RemoveStudentsFromGroupResponse>(group);
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
