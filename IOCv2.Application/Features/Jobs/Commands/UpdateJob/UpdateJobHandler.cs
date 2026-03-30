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

            var applicationsCount = job.InternshipApplications?.Count ?? 0;

            // AC-05: Published + has applications => require confirmation flag
            if (job.Status == JobStatus.PUBLISHED && applicationsCount > 0 && !request.ForceUpdateWithApplications)
            {
                var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.UpdateConfirmHasApplications, applicationsCount);
                // Use Conflict to indicate user action/confirmation required
                return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.Conflict);
            }

            // If job is Closed and being reopened, require a valid new deadline (AC-07)
            var willBeReopened = job.Status == JobStatus.CLOSED;
            if (willBeReopened)
            {
                if (!request.ExpireDate.HasValue || request.ExpireDate.Value.Date < DateTime.UtcNow.Date)
                {
                    var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.ReopenExpireDateInvalid); // "Deadline không hợp lệ..."
                    return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.BadRequest);
                }
            }

            // Business rule: cannot set Quantity lower than already Placed students (AC-07)
            var placedCount = job.InternshipApplications?.Count(a => a.Status == InternshipApplicationStatus.Placed) ?? 0;
            if (request.Quantity.HasValue && request.Quantity.Value < placedCount)
            {
                var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.UpdateQuantityLessThanPlaced, placedCount);
                return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.BadRequest);
            }

            // Begin transaction
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Update scalar properties
                job.Title = request.Title;
                job.Position = request.Position ?? job.Position;
                job.Description = request.Description;
                job.Requirements = request.Requirements;
                job.Benefit = request.Benefit;
                job.Location = request.Location;
                job.Quantity = request.Quantity;
                job.ExpireDate = request.ExpireDate;
                job.StartDate = request.StartDate;
                job.EndDate = request.EndDate;
                job.Audience = request.Audience;
                job.UpdatedAt = DateTime.UtcNow;
                if (Guid.TryParse(_currentUserService.UserId, out var updBy))
                {
                    job.UpdatedBy = updBy;
                }

                if (request.Audience == JobAudience.Targeted)
                {
                    // Update many-to-many: Universities
                    if (request.UniversityIds != null)
                    {
                        // Ensure distinct requested IDs
                        var requestedIds = request.UniversityIds.Distinct().ToList();

                        var universities = (await _unitOfWork.Repository<Domain.Entities.University>()
                            .FindAsync(u => requestedIds.Contains(u.UniversityId), cancellationToken))
                            .ToList();

                        // Determine missing IDs (requested but not found in DB)
                        var foundIds = universities.Select(u => u.UniversityId).ToList();
                        var missingIds = requestedIds.Except(foundIds).ToList();

                        if (missingIds.Any())
                        {
                            // Rollback and return a BadRequest indicating invalid/missing university IDs
                            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                            // Provide a helpful message. Assumes message key exists; falls back to a generic message if not.
                            var msg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.UpdateInvalidUniversities, missingIds.Count, string.Join(',', missingIds))
                                      ?? _messageService.GetMessage(MessageKeys.Common.InvalidRequest);

                            return Result<UpdateJobResponse>.Failure(msg, ResultErrorType.BadRequest);
                        }

                        // Replace the collection
                        job.Universities.Clear();
                        foreach (var u in universities)
                        {
                            job.Universities.Add(u);
                        }
                    }
                    else if (job.Universities == null || !job.Universities.Any())
                    {
                        // If targeted and no universities provided & none exist on entity -> invalid
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.TargetedRequiresSingleUniversity), ResultErrorType.BadRequest);
                    }
                }

                // If closed -> reopen to published after successful update (AC-05 / AC-07)
                if (willBeReopened)
                {
                    job.Status = JobStatus.PUBLISHED;
                }

                await _unitOfWork.Repository<Job>().UpdateAsync(job, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Job {JobId} updated successfully", job.JobId);

                var response = _mapper.Map<UpdateJobResponse>(job);

                // If reopened, notify students who currently have active applications (AC-07)
                if (willBeReopened)
                {
                    var activeStatuses = new[]
                    {
                        InternshipApplicationStatus.Applied,
                        InternshipApplicationStatus.Interviewing,
                        InternshipApplicationStatus.Offered,
                        InternshipApplicationStatus.PendingAssignment // keep safe if used in flows
                    };

                    var activeApplications = job.InternshipApplications?
                        .Where(a => activeStatuses.Contains(a.Status))
                        .ToList() ?? new List<InternshipApplication>();

                    if (activeApplications.Any())
                    {
                        // Notification message (fallback to Vietnamese string per requirement)
                        var notificationMessage = _messageService.GetMessage(
                            MessageKeys.JobPostingMessageKey.ReopenNotifyStudentBody,
                            job.Title,
                            job.Enterprise?.Name ?? string.Empty);

                        if (string.IsNullOrWhiteSpace(notificationMessage) || notificationMessage == MessageKeys.JobPostingMessageKey.ReopenNotifyStudentBody)
                        {
                            notificationMessage = $"Job Posting [{job.Title}] tại {job.Enterprise?.Name ?? string.Empty} đã được cập nhật và mở lại.";
                        }

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

                    var successMsg = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.ReopenSuccess, job.Title);
                    return Result<UpdateJobResponse>.Success(response, successMsg);
                }
                return Result<UpdateJobResponse>.Success(response);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to update job {JobId}", request.JobId);
                return Result<UpdateJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }
    }
}
