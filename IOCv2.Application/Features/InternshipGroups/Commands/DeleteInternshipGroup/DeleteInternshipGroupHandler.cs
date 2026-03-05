using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using IOCv2.Application.Constants;
using AutoMapper;

namespace IOCv2.Application.Features.InternshipGroups.Commands.DeleteInternshipGroup
{
    public class DeleteInternshipGroupHandler : IRequestHandler<DeleteInternshipGroupCommand, Result<DeleteInternshipGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<DeleteInternshipGroupHandler> _logger;

        public DeleteInternshipGroupHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ILogger<DeleteInternshipGroupHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<DeleteInternshipGroupResponse>> Handle(DeleteInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogDeleting), request.InternshipId);

            try
            {
                var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning("{Message}: {InternshipId}", _messageService.GetMessage(MessageKeys.InternshipGroups.NotFound), request.InternshipId);
                    return Result<DeleteInternshipGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                // Delete members first (manual cascade if not handled by DB)
                if (entity.Members.Any())
                {
                    var memberRepo = _unitOfWork.Repository<InternshipStudent>();
                    foreach (var member in entity.Members.ToList())
                    {
                        await memberRepo.DeleteAsync(member);
                    }
                }

                await _unitOfWork.Repository<InternshipGroup>().DeleteAsync(entity);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved > 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
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
                throw;
            }
        }
    }
}
