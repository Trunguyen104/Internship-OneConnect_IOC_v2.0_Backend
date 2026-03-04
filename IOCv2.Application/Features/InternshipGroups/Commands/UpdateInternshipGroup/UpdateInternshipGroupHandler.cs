using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
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

        public UpdateInternshipGroupHandler(
            IUnitOfWork unitOfWork, 
            IMessageService messageService, 
            IMapper mapper,
            ILogger<UpdateInternshipGroupHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<UpdateInternshipGroupResponse>> Handle(UpdateInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating internship group: {InternshipId}", request.InternshipId);

            try
            {
                var entity = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                if (entity == null)
                {
                    _logger.LogWarning("Internship group not found: {InternshipId}", request.InternshipId);
                    return Result<UpdateInternshipGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                // Validate TermId
                var termExists = await _unitOfWork.Repository<Term>()
                    .ExistsAsync(t => t.TermId == request.TermId, cancellationToken);
                if (!termExists)
                {
                    _logger.LogWarning("Term not found: {TermId}", request.TermId);
                    return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.TermNotFound), ResultErrorType.NotFound);
                }

                // Validate EnterpriseId if provided
                if (request.EnterpriseId.HasValue)
                {
                    var enterpriseExists = await _unitOfWork.Repository<Enterprise>()
                        .ExistsAsync(e => e.EnterpriseId == request.EnterpriseId.Value, cancellationToken);
                    if (!enterpriseExists)
                    {
                        _logger.LogWarning("Enterprise not found: {EnterpriseId}", request.EnterpriseId);
                        return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseNotFound), ResultErrorType.NotFound);
                    }
                }

                // Validate MentorId if provided
                if (request.MentorId.HasValue)
                {
                    var mentorExists = await _unitOfWork.Repository<User>()
                        .ExistsAsync(u => u.UserId == request.MentorId.Value, cancellationToken);
                    if (!mentorExists)
                    {
                        _logger.LogWarning("Mentor not found: {MentorId}", request.MentorId);
                        return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.MentorNotFound), ResultErrorType.NotFound);
                    }
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                entity.UpdateInfo(
                    request.GroupName,
                    request.TermId,
                    request.EnterpriseId,
                    request.MentorId,
                    request.StartDate,
                    request.EndDate
                );

                await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(entity);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved > 0 || !_unitOfWork.Repository<InternshipGroup>().Query().Any(x => x.InternshipId == request.InternshipId))
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    _logger.LogInformation("Successfully updated internship group: {InternshipId}", entity.InternshipId);
                    
                    var response = _mapper.Map<UpdateInternshipGroupResponse>(entity);
                    return Result<UpdateInternshipGroupResponse>.Success(response);
                }

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError("Failed to update internship group in database");
                return Result<UpdateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while updating internship group");
                throw;
            }
        }
    }
}
