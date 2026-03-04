using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.DeleteStakeholderIssue
{
    public class DeleteStakeholderIssueCommandHandler : IRequestHandler<DeleteStakeholderIssueCommand, Result<DeleteStakeholderIssueResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<DeleteStakeholderIssueCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;

        public DeleteStakeholderIssueCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<DeleteStakeholderIssueCommandHandler> logger,
            ICurrentUserService currentUserService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<DeleteStakeholderIssueResponse>> Handle(DeleteStakeholderIssueCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting StakeholderIssue {Id}", request.Id);

            var issue = await _unitOfWork.Repository<StakeholderIssue>()
                .Query()
                .FirstOrDefaultAsync(si => si.Id == request.Id, cancellationToken);

            if (issue == null)
            {
                _logger.LogWarning("StakeholderIssue {Id} not found for deletion", request.Id);
                return Result<DeleteStakeholderIssueResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Issue.NotFound));
            }

            try
            {
                await _unitOfWork.Repository<StakeholderIssue>().HardDeleteAsync(issue, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                _logger.LogInformation("Successfully deleted StakeholderIssue {Id}", request.Id);

                var response = _mapper.Map<DeleteStakeholderIssueResponse>(issue);
                return Result<DeleteStakeholderIssueResponse>.Success(
                    response,
                    _messageService.GetMessage(MessageKeys.Issue.DeleteSuccess)
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting StakeholderIssue {Id}", request.Id);
                throw;
            }
        }
    }
}
