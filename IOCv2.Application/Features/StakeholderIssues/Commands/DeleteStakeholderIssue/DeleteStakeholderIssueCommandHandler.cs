using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue
{
    public class DeleteStakeholderIssueCommandHandler : IRequestHandler<DeleteStakeholderIssueCommand, Result<DeleteStakeholderIssueResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public DeleteStakeholderIssueCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<DeleteStakeholderIssueResponse>> Handle(DeleteStakeholderIssueCommand request, CancellationToken cancellationToken)
        {
            // Find issue
            var issue = await _unitOfWork.Repository<StakeholderIssue>()
                .Query()
                .FirstOrDefaultAsync(si => si.Id == request.Id, cancellationToken);

            if (issue == null)
                return Result<DeleteStakeholderIssueResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Issue.NotFound));

            // Hard delete
            await _unitOfWork.Repository<StakeholderIssue>().HardDeleteAsync(issue, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            
            issue.DeletedAt = DateTime.UtcNow;

            // Return response
            var response = _mapper.Map<DeleteStakeholderIssueResponse>(issue);
            return Result<DeleteStakeholderIssueResponse>.Success(
                response,
                _messageService.GetMessage(MessageKeys.Issue.DeleteSuccess)
            );
        }
    }
}

