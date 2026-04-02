using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.StakeholderIssues.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.StakeholderIssues.Commands.CreateStakeholderIssue
{
    public class CreateStakeholderIssueCommandHandler : IRequestHandler<CreateStakeholderIssueCommand, Result<CreateStakeholderIssueResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<CreateStakeholderIssueCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public CreateStakeholderIssueCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<CreateStakeholderIssueCommandHandler> logger,
            ICurrentUserService currentUserService,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
        }

        public async Task<Result<CreateStakeholderIssueResponse>> Handle(CreateStakeholderIssueCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Creating StakeholderIssue for Stakeholder {StakeholderId} (Title: {Title})", 
                request.StakeholderId, request.Title);

            // Fetch stakeholder with project info for security check
            var stakeholder = await _unitOfWork.Repository<Stakeholder>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == request.StakeholderId, cancellationToken);

            if (stakeholder == null)
            {
                _logger.LogWarning("Stakeholder {StakeholderId} not found", request.StakeholderId);
                return Result<CreateStakeholderIssueResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Issue.StakeholderNotFound));
            }

            // Security Check: Verify user belongs to the project
            // Replace with actual ICurrentUserService check if available, or placeholder for now
            // var userId = _currentUserService.UserId;
            // TODO: check project membership

            // Create entity using rich domain constructor
            var issue = new StakeholderIssue(
                Guid.NewGuid(),
                request.StakeholderId,
                request.Title.Trim(),
                request.Description.Trim()
            );

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                await _unitOfWork.Repository<StakeholderIssue>().AddAsync(issue, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(StakeholderIssueCacheKeys.IssueListPattern(), cancellationToken);

                _logger.LogInformation("Successfully created StakeholderIssue {Id} for Stakeholder {StakeholderId}",
                    issue.Id, request.StakeholderId);

                var response = _mapper.Map<CreateStakeholderIssueResponse>(issue);
                return Result<CreateStakeholderIssueResponse>.Success(
                    response,
                    _messageService.GetMessage(MessageKeys.Issue.CreateSuccess)
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while creating StakeholderIssue for Stakeholder {StakeholderId}", request.StakeholderId);
                return Result<CreateStakeholderIssueResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}
