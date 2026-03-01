using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus
{
    public class UpdateStakeholderIssueStatusCommandHandler : IRequestHandler<UpdateStakeholderIssueStatusCommand, Result<UpdateStakeholderIssueStatusResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public UpdateStakeholderIssueStatusCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<UpdateStakeholderIssueStatusResponse>> Handle(UpdateStakeholderIssueStatusCommand request, CancellationToken cancellationToken)
        {
            // Find issue
            var issue = await _unitOfWork.Repository<StakeholderIssue>()
                .Query()
                .FirstOrDefaultAsync(si => si.Id == request.Id, cancellationToken);

            if (issue == null)
                return Result<UpdateStakeholderIssueStatusResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Issue.NotFound));

            // Parse status string to enum
            if (!Enum.TryParse<StakeholderIssueStatus>(request.Status, true, out var status))
                return Result<UpdateStakeholderIssueStatusResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Issue.InvalidStatus),
                    ResultErrorType.BadRequest);

            // Update status
            issue.Status = status;

            // Handle ResolvedAt based on status
            if (status == StakeholderIssueStatus.Resolved || status == StakeholderIssueStatus.Closed)
            {
                issue.ResolvedAt = DateTime.UtcNow;
            }
            else // Open or InProgress
            {
                issue.ResolvedAt = null;
            }

            // Persist
            await _unitOfWork.Repository<StakeholderIssue>().UpdateAsync(issue, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            
            issue.UpdatedAt = DateTime.UtcNow;

            // Return response
            var response = _mapper.Map<UpdateStakeholderIssueStatusResponse>(issue);
            return Result<UpdateStakeholderIssueStatusResponse>.Success(
                response,
                _messageService.GetMessage(MessageKeys.Issue.UpdateStatusSuccess)
            );
        }
    }
}

