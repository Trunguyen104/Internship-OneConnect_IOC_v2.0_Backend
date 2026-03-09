using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus
{
    public class UpdateStakeholderIssueStatusCommandHandler : IRequestHandler<UpdateStakeholderIssueStatusCommand, Result<UpdateStakeholderIssueStatusResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<UpdateStakeholderIssueStatusCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public UpdateStakeholderIssueStatusCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<UpdateStakeholderIssueStatusCommandHandler> logger,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<UpdateStakeholderIssueStatusResponse>> Handle(UpdateStakeholderIssueStatusCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating status for StakeholderIssue {Id} to {Status}", request.Id, request.Status);

            var issue = await _unitOfWork.Repository<StakeholderIssue>()
                .Query()
                .FirstOrDefaultAsync(si => si.Id == request.Id, cancellationToken);

            if (issue == null)
            {
                _logger.LogWarning("StakeholderIssue {Id} not found", request.Id);
                return Result<UpdateStakeholderIssueStatusResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Issue.NotFound));
            }

            issue.UpdateStatus(request.Status);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                await _unitOfWork.Repository<StakeholderIssue>().UpdateAsync(issue, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Successfully updated StakeholderIssue {Id} status to {Status}", request.Id, request.Status);

                var response = _mapper.Map<UpdateStakeholderIssueStatusResponse>(issue);
                return Result<UpdateStakeholderIssueStatusResponse>.Success(
                    response,
                    _messageService.GetMessage(MessageKeys.Issue.UpdateStatusSuccess)
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while updating status for StakeholderIssue {Id}", request.Id);
                return Result<UpdateStakeholderIssueStatusResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}
