using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Jobs;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.UpdateJob
{
    public class UpdateJobHandler : IRequestHandler<UpdateJobCommand, Result<UpdateJobResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<UpdateJobHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IPublisher _publisher;

        public UpdateJobHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<UpdateJobHandler> logger,
            IMapper mapper,
            IPublisher publisher)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
            _mapper = mapper;
            _publisher = publisher;
        }

        public async Task<Result<UpdateJobResponse>> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Updating job {JobId} by user {UserId}", request.JobId, _currentUserService.UserId);

            // Load job with related collections (including student/user for notifications)
            var job = await _unitOfWork.Repository<Job>()
                .Query()
                .Include(j => j.Universities)
                .Include(j => j.InternshipApplications)
                    .ThenInclude(a => a.Student)
                        .ThenInclude(s => s.User)
                .Include(j => j.Enterprise)
                .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("Job not found: {JobId}", request.JobId);
                return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.RecordNotFound), ResultErrorType.NotFound);
            }

            // Security / ownership: only HR belonging to the enterprise can update
            if (JobsPostingParam.GetJobPostings.EnterpriseRoles.Contains(_currentUserService.Role))
            {
                if (!Guid.TryParse(_currentUserService.UserId, out var userId))
                {
                    return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
                }

                var entUser = await _unitOfWork.Repository<EnterpriseUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.UserId == userId, cancellationToken);

                if (entUser == null || entUser.EnterpriseId != job.EnterpriseId)
                {
                    return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
                }
            }
            else
            {
                // Non-enterprise users are not allowed to update jobs
                return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }

            // Count applications and placed count here for branch decisions
            var applicationsCount = job.InternshipApplications?.Count ?? 0;
            var placedCount = job.InternshipApplications?.Count(a => a.Status == InternshipApplicationStatus.Placed) ?? 0;
            if (job.Quantity < placedCount)
            {
                return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.QuantityCannotBeLessThanPlaced), ResultErrorType.BadRequest);
            }
            // Route handling explicitly by status.
            // Per AC-05 requirements we handle Draft, Published(with/without applications), and Closed separately.
            switch (job.Status)
            {
                case JobStatus.DRAFT:
                    return await HandleDraftAsync(job, request, cancellationToken);

                case JobStatus.PUBLISHED:
                    if (applicationsCount == 0)
                    {
                        return await HandlePublishedNoAppsAsync(job, request, cancellationToken);
                    }
                    else
                    {
                        return await HandlePublishedWithAppsAsync(job, request, cancellationToken);
                    }

                case JobStatus.CLOSED:
                    return await HandleClosedAsync(job, request, cancellationToken);

                default:
                    // If some other status exists, fall back to update behavior (conservative)
                    return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InvalidRequest), ResultErrorType.BadRequest);
            }
        }

        // DRAFT: No active applications allowed. Allow changing intern phase and other fields.
        private async Task<Result<UpdateJobResponse>> HandleDraftAsync(Job job, UpdateJobCommand request, CancellationToken cancellationToken)
        {
            // Draft job must not have any applications (active or not)
            if ((job.InternshipApplications?.Any() ?? false))
            {
                var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.DraftNoApplications);
                return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.BadRequest);
            }

            // Begin transaction and perform update (Draft -> remain Draft or other status depending on request.Status)
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                UpdateJobScalars(job, request);

                // Universities handling when Audience == Targeted
                var universityValidation = await HandleUniversitiesForAudienceAsync(job, request, cancellationToken);
                if (!universityValidation.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return universityValidation;
                }

                // Apply requested status (Draft may remain Draft or change)
                job.Status = request.Status;

                await _unitOfWork.Repository<Job>().UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Draft job {JobId} updated successfully", job.JobId);
                var response = _mapper.Map<UpdateJobResponse>(job);
                return Result<UpdateJobResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update draft job {JobId}", job.JobId);
                return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }

        // PUBLISHED without applications: allow full update including InternshipPhase change.
        private async Task<Result<UpdateJobResponse>> HandlePublishedNoAppsAsync(Job job, UpdateJobCommand request, CancellationToken cancellationToken)
        {
            // Business rule: cannot set Quantity lower than already Placed students
            var placedCount = job.InternshipApplications?.Count(a => a.Status == InternshipApplicationStatus.Placed) ?? 0;
            if (request.Quantity.HasValue && request.Quantity.Value < placedCount)
            {
                var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.UpdateQuantityLessThanPlaced, placedCount);
                return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.BadRequest);
            }

            // No confirmation required because there are no active applications
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                UpdateJobScalars(job, request);

                var universityValidation = await HandleUniversitiesForAudienceAsync(job, request, cancellationToken);
                if (!universityValidation.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return universityValidation;
                }

                // Keep status Published unless request.Status asks otherwise
                job.Status = request.Status;

                await _unitOfWork.Repository<Job>().UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Published job (no apps) {JobId} updated successfully", job.JobId);
                var response = _mapper.Map<UpdateJobResponse>(job);
                return Result<UpdateJobResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update published (no apps) job {JobId}", job.JobId);
                return Result<UpdateJobResponse>.Failure(ex.Message, ResultErrorType.InternalServerError);
            }
        }

        // PUBLISHED with active applications: block InternPhase change; other fields require confirmation (ForceUpdateWithApplications)
        private async Task<Result<UpdateJobResponse>> HandlePublishedWithAppsAsync(Job job, UpdateJobCommand request, CancellationToken cancellationToken)
        {
            var applicationsCount = job.InternshipApplications?.Count ?? 0;

            // If intern phase change requested -> BLOCK (AC-05)
            var existingPhaseId = job.InternshipPhaseId;
            var incomingPhaseId = request.InternshipPhaseId;
            var internPhaseChanged = (existingPhaseId != incomingPhaseId);

            if (internPhaseChanged)
            {
                // Return business error message blocking the change
                var blockedMsg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.ChangeInternPhaseBlockedDueToActiveApplications, applicationsCount);
                return Result<UpdateJobResponse>.Failure(blockedMsg, ResultErrorType.BadRequest);
            }

            // Other field changes need user confirmation (ForceUpdateWithApplications)
            if (!request.ForceUpdateWithApplications)
            {
                // Provide a confirmation conflict result to let frontend ask HR to confirm
                var confirmMsg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.UpdateConfirmHasApplications, applicationsCount);
                return Result<UpdateJobResponse>.Failure(confirmMsg, ResultErrorType.Conflict);
            }

            // Upon confirmation, validate quantity vs placed
            var placedCount = job.InternshipApplications?.Count(a => a.Status == InternshipApplicationStatus.Placed) ?? 0;
            if (request.Quantity.HasValue && request.Quantity.Value < placedCount)
            {
                var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.UpdateQuantityLessThanPlaced, placedCount);
                return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.BadRequest);
            }

            // Proceed with update and notify active applicants after successful update
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                UpdateJobScalars(job, request);

                var universityValidation = await HandleUniversitiesForAudienceAsync(job, request, cancellationToken);
                if (!universityValidation.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return universityValidation;
                }

                // Keep Published status
                job.Status = JobStatus.PUBLISHED;

                await _unitOfWork.Repository<Job>().UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Published job (with apps) {JobId} updated successfully", job.JobId);
                var response = _mapper.Map<UpdateJobResponse>(job);

                var activeApplications = job.InternshipApplications?
                    .Where(a => JobsPostingParam.UpdateJobPosting.ActiveStatuses.Contains(a.Status))
                    .ToList() ?? new List<InternshipApplication>();

                if (activeApplications.Any())
                {
                    var notificationMessage = _messageService.GetMessage(
                        MessageKeys.JobPostingMessageKey.UpdateNotifyStudentBody,
                        job.Title ?? string.Empty,
                        job.Enterprise?.Name ?? string.Empty);

                    var distinctUsers = activeApplications
                        .Select(a => a.Student?.User)
                        .Where(u => u != null && u.UserId != Guid.Empty)
                        .GroupBy(u => u!.UserId)
                        .Select(g => g.First()!)
                        .ToList();

                    foreach (var user in distinctUsers)
                    {
                        try
                        {
                            await _publisher.Publish(new JobReopenedEvent(
                                user.UserId,
                                job.JobId,
                                job.Title ?? string.Empty,
                                job.Enterprise?.Name ?? string.Empty,
                                notificationMessage), cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to publish Update notification for user {UserId} and job {JobId}", user.UserId, job.JobId);
                        }
                    }
                }

                return Result<UpdateJobResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update published (with apps) job {JobId}", job.JobId);
                return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }

        // CLOSED: reopen to Published after successful validation. Require new valid ExpireDate.
        private async Task<Result<UpdateJobResponse>> HandleClosedAsync(Job job, UpdateJobCommand request, CancellationToken cancellationToken)
        {
            // AC-07: When reopening, require a valid new deadline
            if (!request.ExpireDate.HasValue || request.ExpireDate.Value.Date < DateTime.UtcNow.Date)
            {
                var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.ReopenExpireDateInvalid);
                return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.BadRequest);
            }

            // Business rule: cannot set Quantity lower than already Placed students
            var placedCount = job.InternshipApplications?.Count(a => a.Status == InternshipApplicationStatus.Placed) ?? 0;
            if (request.Quantity.HasValue && request.Quantity.Value < placedCount)
            {
                var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.UpdateQuantityLessThanPlaced, placedCount);
                return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.BadRequest);
            }

            // Proceed to reopen and notify active applicants
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                UpdateJobScalars(job, request);

                var universityValidation = await HandleUniversitiesForAudienceAsync(job, request, cancellationToken);
                if (!universityValidation.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return universityValidation;
                }

                // Reopen to Published
                job.Status = JobStatus.PUBLISHED;

                await _unitOfWork.Repository<Job>().UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Closed job {JobId} reopened and updated successfully", job.JobId);

                var response = _mapper.Map<UpdateJobResponse>(job);

                var activeApplications = job.InternshipApplications?
                    .Where(a => JobsPostingParam.UpdateJobPosting.ActiveStatuses.Contains(a.Status))
                    .ToList() ?? new List<InternshipApplication>();

                if (activeApplications.Any())
                {
                    var notificationMessage = _messageService.GetMessage(
                        MessageKeys.JobPostingMessageKey.ReopenNotifyStudentBody,
                        job.Title!,
                        job.Enterprise?.Name ?? string.Empty);

                    var distinctUsers = activeApplications
                        .Select(a => a.Student?.User)
                        .Where(u => u != null && u.UserId != Guid.Empty)
                        .GroupBy(u => u!.UserId)
                        .Select(g => g.First()!)
                        .ToList();

                    foreach (var user in distinctUsers)
                    {
                        try
                        {
                            await _publisher.Publish(new JobReopenedEvent(
                                user.UserId,
                                job.JobId,
                                job.Title ?? string.Empty,
                                job.Enterprise?.Name ?? string.Empty,
                                notificationMessage), cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to publish JobReopenedEvent for user {UserId} and job {JobId}", user.UserId, job.JobId);
                        }
                    }
                }

                var successMsg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.ReopenSuccess, job.Title!);
                return Result<UpdateJobResponse>.Success(response, successMsg);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to reopen closed job {JobId}", job.JobId);
                return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }

        // Update scalar properties shared across branches (keeps property assignment consistent).
        // Note: We still call this helper inside each branch — the branch contains its own validations and transaction.
        private void UpdateJobScalars(Job job, UpdateJobCommand request)
        {
            job.Title = request.Title;
            job.Position = request.Position ?? job.Position;
            job.Description = request.Description;
            job.Requirements = request.Requirements;
            job.Benefit = request.Benefit;
            job.Location = request.Location;
            job.Quantity = request.Quantity;
            job.ExpireDate = request.ExpireDate;
            job.Audience = request.Audience;
            job.UpdatedAt = DateTime.UtcNow;
            if (Guid.TryParse(_currentUserService.UserId, out var updBy))
            {
                job.UpdatedBy = updBy;
            }

            // InternshipPhaseId assignment allowed depending on branch rules (published with apps blocks change),
            // here we simply apply incoming value — branches that must block already prevented change.
            job.InternshipPhaseId = request.InternshipPhaseId;
        }

        // Handle targeted audience universities. Returns a Result that can be Failure tuple with proper message.
        private async Task<Result<UpdateJobResponse>> HandleUniversitiesForAudienceAsync(Job job, UpdateJobCommand request, CancellationToken cancellationToken)
        {
            if (request.Audience == JobAudience.Targeted)
            {
                if (request.UniversityIds != null)
                {
                    var requestedIds = request.UniversityIds.Distinct().ToList();

                    var universities = (await _unitOfWork.Repository<Domain.Entities.University>()
                        .FindAsync(u => requestedIds.Contains(u.UniversityId), cancellationToken))
                        .ToList();

                    var foundIds = universities.Select(u => u.UniversityId).ToList();
                    var missingIds = requestedIds.Except(foundIds).ToList();

                    if (missingIds.Any())
                    {
                        var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.UpdateInvalidUniversities, missingIds.Count, string.Join(',', missingIds))
                                  ?? _messageService.GetMessage(MessageKeys.Common.InvalidRequest);
                        return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.BadRequest);
                    }

                    job.Universities.Clear();
                    foreach (var u in universities)
                    {
                        job.Universities.Add(u);
                    }
                }
                else if (job.Universities == null || !job.Universities.Any())
                {
                    var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.TargetedRequiresSingleUniversity);
                    return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.BadRequest);
                }
            }

            return Result<UpdateJobResponse>.Success(null!);
        }
    }
}
