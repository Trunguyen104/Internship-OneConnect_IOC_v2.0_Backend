using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.StakeholderIssues.Common;
using IOCv2.Application.Features.Stakeholders.Common;
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
        private readonly ICacheService _cacheService;

        public DeleteStakeholderIssueCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<DeleteStakeholderIssueCommandHandler> logger,
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

        public async Task<Result<DeleteStakeholderIssueResponse>> Handle(DeleteStakeholderIssueCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting StakeholderIssue {Id}", request.Id);

            var authError = StakeholderAccessGuard.EnsureAuthenticated<DeleteStakeholderIssueResponse>(_currentUserService, _messageService);
            if (authError is not null)
            {
                return authError;
            }


            var issue = await _unitOfWork.Repository<StakeholderIssue>()
                .Query()
                .Include(si => si.Stakeholder)
                .FirstOrDefaultAsync(si => si.Id == request.Id, cancellationToken);

            if (issue == null)
            {
                _logger.LogWarning("StakeholderIssue {Id} not found for deletion", request.Id);
                return Result<DeleteStakeholderIssueResponse>.NotFound(
                    _messageService.GetMessage(MessageKeys.Issue.NotFound));
            }

            var internshipId = issue.Stakeholder?.InternshipId;
            if (!internshipId.HasValue || internshipId.Value == Guid.Empty)
            {
                internshipId = await _unitOfWork.Repository<Stakeholder>()
                    .Query()
                    .AsNoTracking()
                    .Where(s => s.Id == issue.StakeholderId)
                    .Select(s => (Guid?)s.InternshipId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (!internshipId.HasValue || internshipId.Value == Guid.Empty)
            {
                return Result<DeleteStakeholderIssueResponse>.NotFound(_messageService.GetMessage(MessageKeys.Issue.StakeholderNotFound));
            }

            var accessError = await StakeholderAccessGuard.EnsureInternshipAccessAsync<DeleteStakeholderIssueResponse>(
                _unitOfWork,
                _messageService,
                _currentUserService,
                internshipId.Value,
                cancellationToken);

            if (accessError is not null)
            {
                _logger.LogWarning("User {UserId} attempted to delete issue {IssueId} without permission in internship {InternshipId}", _currentUserService.UserId, request.Id, internshipId.Value);
                return accessError;
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                await _unitOfWork.Repository<StakeholderIssue>().HardDeleteAsync(issue, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(StakeholderIssueCacheKeys.IssueListPattern(), cancellationToken);
                await _cacheService.RemoveAsync(StakeholderIssueCacheKeys.Issue(request.Id), cancellationToken);

                _logger.LogInformation("Successfully deleted StakeholderIssue {Id}", request.Id);

                var response = _mapper.Map<DeleteStakeholderIssueResponse>(issue);
                return Result<DeleteStakeholderIssueResponse>.Success(
                    response,
                    _messageService.GetMessage(MessageKeys.Issue.DeleteSuccess)
                );
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error occurred while deleting StakeholderIssue {Id}", request.Id);
                return Result<DeleteStakeholderIssueResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.Conflict);
            }
        }
    }
}
