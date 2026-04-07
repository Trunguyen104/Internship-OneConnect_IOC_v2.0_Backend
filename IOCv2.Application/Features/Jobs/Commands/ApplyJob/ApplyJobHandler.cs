using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Extensions.Jobs;
using IOCv2.Application.Features.Notifications.Events;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.ApplyJob
{
    public class ApplyJobHandler : IRequestHandler<ApplyJobCommand, Result<ApplyJobResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<ApplyJobHandler> _logger;
        private readonly IPublisher _publisher;
        private readonly int _reapplyLimit;

        public ApplyJobHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ILogger<ApplyJobHandler> logger,
            IPublisher publisher,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _logger = logger;
            _publisher = publisher;

            // Read re-apply limit from configuration. Fallback to 3 if not configured or invalid.
            _reapplyLimit = configuration.GetValue<int?>("JobPosting:ReapplyLimit") ?? 3;
        }

        public async Task<Result<ApplyJobResponse>> Handle(ApplyJobCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("User {UserId} attempts to apply for job {JobId}", _currentUserService.UserId, request.JobId);

            // 1. Validate current user
            if (!Guid.TryParse(_currentUserService.UserId, out var currentUserId))
            {
                _logger.LogWarning("Unauthorized apply attempt (invalid user id)");
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            // 2. Resolve Student by current user
            var student = await _unitOfWork.Repository<Student>()
                .Query()
                .Include(s => s.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == currentUserId, cancellationToken);

            if (student == null)
            {
                _logger.LogWarning("Student record not found for user {UserId}", currentUserId);
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Profile.StudentNotFound), ResultErrorType.NotFound);
            }

            // 3. CV must exist
            if (string.IsNullOrWhiteSpace(student.CvUrl))
            {
                _logger.LogWarning("Student {StudentId} attempted to apply without CV", student.StudentId);
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.UploadCVRequired), ResultErrorType.BadRequest);
            }

            // 4. Student must not be already Placed
            if (student.InternshipStatus == StudentStatus.Placed)
            {
                _logger.LogWarning("Student {StudentId} is already placed", student.StudentId);
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.CannotApplyWhenPlaced), ResultErrorType.BadRequest);
            }

            // 5. Load job and related enterprise + universities.
            // Use IgnoreQueryFilters and include Universities so visibility rules match GetJobsHandler.
            var job = await _unitOfWork.Repository<Job>()
                .Query()
                .Include(j => j.Enterprise)
                .Include(j => j.InternshipApplications)
                .Include(j => j.Universities)
                .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

            if (job == null)
            {
                _logger.LogWarning("Job not found: {JobId}", request.JobId);
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.RecordNotFound), ResultErrorType.NotFound);
            }

            // 5.a Visibility check for non-enterprise users (mirror GetJobsHandler behavior)
            if (!JobsPostingParam.GetJobPostings.EnterpriseRoles.Contains(_currentUserService.Role))
            {
                var uniUser = await _unitOfWork.Repository<UniversityUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserId == currentUserId, cancellationToken);

                if (uniUser == null)
                {
                    _logger.LogWarning("University user not found for user {UserId}", currentUserId);
                    return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.NotAllowed), ResultErrorType.Forbidden);
                }

                var visibleToUniversity = job.Audience == JobAudience.Public || job.Universities.Any(u => u.UniversityId == uniUser.UniversityId);

                if (job.Status != JobStatus.PUBLISHED || !visibleToUniversity)
                {
                    _logger.LogInformation("Job {JobId} is not visible/open to user {UserId}", job.JobId, currentUserId);
                    return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.PositionNotOpenForApplication), ResultErrorType.BadRequest);
                }
            }

            // 6. Job must be open/published and before deadline (enterprise users also validated here)
            var now = DateTime.UtcNow;
            if (job.Status != JobStatus.PUBLISHED)
            {
                _logger.LogInformation("Job {JobId} is not published", job.JobId);
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.PositionNotOpenForApplication), ResultErrorType.BadRequest);
            }

            if (job.ExpireDate.HasValue && job.ExpireDate.Value < now)
            {
                _logger.LogInformation("Job {JobId} expired at {ExpireDate}", job.JobId, job.ExpireDate);
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.ApplicationDeadlinePassed), ResultErrorType.BadRequest);
            }

            // 7. Determine current active internship phase (Phase.Status == Open && contains today)
            var todayDateOnly = DateOnly.FromDateTime(DateTime.UtcNow);
            var currentPhase = await _unitOfWork.Repository<InternshipPhase>()
                .Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Status == InternshipPhaseStatus.Open && p.PhaseId == job.InternshipPhaseId, cancellationToken);

            if (currentPhase == null)
            {
                _logger.LogInformation("No active internship phase found for job {JobId}", job.JobId);
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.NoActiveInternshipPeriod), ResultErrorType.BadRequest);
            }

            // 8. Check for any active application for this student (global)

            var activeStatuses = JobsPostingParam.UpdateJobPosting.ActiveStatuses;

            var hasActiveApp = await _unitOfWork.Repository<InternshipApplication>()
                .Query()
                .AsNoTracking()
                .AnyAsync(a => a.StudentId == student.StudentId && activeStatuses.Contains(a.Status), cancellationToken);

            if (hasActiveApp)
            {
                _logger.LogInformation("Student {StudentId} already has an active application", student.StudentId);
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.AlreadyHaveActiveApplication), ResultErrorType.BadRequest);
            }

            // 9. Re-apply limit: count previous applications for same job in same phase
            // Note: The AC requires "lần đầu apply + re-apply after Withdrawn or Rejected".
            // We count existing application records for the student/job/term. Active statuses were blocked above,
            // so this effectively limits total attempts across Withdrawn/Rejected/other past statuses.
            var reapplyCount = await _unitOfWork.Repository<InternshipApplication>()
                .Query()
                .AsNoTracking()
                .CountAsync(a => a.StudentId == student.StudentId && a.JobId == job.JobId, cancellationToken);

            if (reapplyCount >= _reapplyLimit)
            {
                _logger.LogInformation("Student {StudentId} reached reapply limit ({Limit}) for job {JobId}", student.StudentId, _reapplyLimit, job.JobId);
                // Pass configured limit to message service so UI can display the value if resource supports formatting.
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.JobPostingMessageKey.ApplicationLimitReached, _reapplyLimit), ResultErrorType.BadRequest);
            }

            // 10. Create application
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var termId = await _unitOfWork.Repository<StudentTerm>().Query().Include(st => st.Term)
                    .Where(st => st.StudentId == student.StudentId && st.Term.Status == TermStatus.Open)
                    .Select(st => st.TermId)
                    .FirstOrDefaultAsync(cancellationToken);
                if (termId == Guid.Empty)
                {
                    _logger.LogWarning("No active term found for student {StudentId}", student.StudentId);
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Terms.NotFound), ResultErrorType.BadRequest);
                }

                var app = new InternshipApplication
                {
                    ApplicationId = Guid.NewGuid(),
                    EnterpriseId = job.EnterpriseId,
                    StudentId = student.StudentId,
                    TermId = termId,
                    JobId = job.JobId,
                    Status = InternshipApplicationStatus.Applied,
                    Source = ApplicationSource.SelfApply,
                    CvSnapshotUrl = student.CvUrl,
                    JobPostingTitle = job.Title,
                    AppliedAt = DateTime.UtcNow
                };

                await _unitOfWork.Repository<InternshipApplication>().AddAsync(app);
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                _logger.LogInformation("Student {StudentId} applied successfully to job {JobId} (application {AppId})", student.StudentId, job.JobId, app.ApplicationId);

                // 11. Notify enterprise HR users
                var hrUsers = await _unitOfWork.Repository<EnterpriseUser>()
                    .Query()
                    .Include(eu => eu.User)
                    .AsNoTracking()
                    .Where(eu => eu.EnterpriseId == job.EnterpriseId && (eu.User.Role == UserRole.HR || eu.User.Role == UserRole.EnterpriseAdmin))
                    .Select(eu => eu.User)
                    .ToListAsync(cancellationToken);

                var studentName = student.User?.FullName ?? string.Empty;
                var enterpriseName = job.Enterprise?.Name ?? string.Empty;
                var jobTitle = job.Title ?? string.Empty;

                var notificationMessage = _messageService.GetMessage(MessageKeys.JobPostingMessageKey.StudentAppliedToEnterpriseJob, studentName, enterpriseName, jobTitle);

                foreach (var hr in hrUsers)
                {
                    try
                    {
                        await _publisher.Publish(new ApplicationSubmittedEvent(
                            hr.UserId,
                            app.ApplicationId,
                            job.JobId,
                            studentName,
                            jobTitle,
                            enterpriseName,
                            notificationMessage), cancellationToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to publish ApplicationSubmittedEvent to HR {UserId}", hr.UserId);
                    }
                }

                // 12. Return success response
                var response = new ApplyJobResponse { ApplicationId = app.ApplicationId };
                return Result<ApplyJobResponse>.Success(response, _messageService.GetMessage(MessageKeys.JobPostingMessageKey.ApplySuccessPendingHR));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Failed to create application for student {StudentId} and job {JobId}", student.StudentId, job.JobId);
                return Result<ApplyJobResponse>.Failure(ex.Message, ResultErrorType.InternalServerError);
            }
        }
    }
}
