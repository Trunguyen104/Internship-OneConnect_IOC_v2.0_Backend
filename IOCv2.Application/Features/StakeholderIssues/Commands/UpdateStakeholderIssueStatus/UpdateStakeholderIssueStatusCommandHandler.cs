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

namespace IOCv2.Application.Features.StakeholderIssues.Commands.UpdateStakeholderIssueStatus
{
    public class UpdateStakeholderIssueStatusCommandHandler : IRequestHandler<UpdateStakeholderIssueStatusCommand, Result<UpdateStakeholderIssueStatusResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<UpdateStakeholderIssueStatusCommandHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public UpdateStakeholderIssueStatusCommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<UpdateStakeholderIssueStatusCommandHandler> logger,
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

        public async Task<Result<UpdateStakeholderIssueStatusResponse>> Handle(UpdateStakeholderIssueStatusCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating status for StakeholderIssue {Id} to {Status}", request.Id, request.Status);

            var authError = StakeholderAccessGuard.EnsureAuthenticated<UpdateStakeholderIssueStatusResponse>(_currentUserService, _messageService);
            if (authError is not null)
            {
                return authError;
            }

            var managePermissionError = StakeholderAccessGuard.EnsureManagePermission<UpdateStakeholderIssueStatusResponse>(_currentUserService, _messageService);
            if (managePermissionError is not null)
            {
                _logger.LogWarning("User {UserId} with role {Role} attempted to update stakeholder issue {IssueId}", _currentUserService.UserId, _currentUserService.Role, request.Id);
                return managePermissionError;
            }

            var issue = await _unitOfWork.Repository<StakeholderIssue>()
                .Query()
                .Include(si => si.Stakeholder)
                .FirstOrDefaultAsync(si => si.Id == request.Id, cancellationToken);

            if (issue == null)
            {
                _logger.LogWarning("StakeholderIssue {Id} not found", request.Id);
                return Result<UpdateStakeholderIssueStatusResponse>.NotFound(
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
                return Result<UpdateStakeholderIssueStatusResponse>.NotFound(_messageService.GetMessage(MessageKeys.Issue.StakeholderNotFound));
            }

            var accessError = await StakeholderAccessGuard.EnsureInternshipAccessAsync<UpdateStakeholderIssueStatusResponse>(
                _unitOfWork,
                _messageService,
                _currentUserService,
                internshipId.Value,
                cancellationToken);

            if (accessError is not null)
            {
                _logger.LogWarning("User {UserId} attempted to update issue {IssueId} without permission in internship {InternshipId}", _currentUserService.UserId, request.Id, internshipId.Value);
                return accessError;
            }

            issue.UpdateStatus(request.Status);

            try
            {
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                await _unitOfWork.Repository<StakeholderIssue>().UpdateAsync(issue, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                await _cacheService.RemoveByPatternAsync(StakeholderIssueCacheKeys.IssueListPattern(), cancellationToken);
                await _cacheService.RemoveAsync(StakeholderIssueCacheKeys.Issue(request.Id), cancellationToken);

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
