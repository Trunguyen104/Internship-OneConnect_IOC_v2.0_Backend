using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Jobs.Commands.ApplyJob
{
    public class ApplyJobHandler : MediatR.IRequestHandler<ApplyJobCommand, Result<ApplyJobResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<ApplyJobHandler> _logger;
        private readonly ICurrentUserService _currentUserService;
        private readonly IBackgroundEmailSender _backgroundEmailSender;
        private readonly IConfiguration _configuration;

        public ApplyJobHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            ILogger<ApplyJobHandler> logger,
            ICurrentUserService currentUserService,
            IBackgroundEmailSender backgroundEmailSender,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _currentUserService = currentUserService;
            _backgroundEmailSender = backgroundEmailSender;
            _configuration = configuration;
        }

        public async Task<Result<ApplyJobResponse>> Handle(ApplyJobCommand request, CancellationToken cancellationToken)
        {
            try
            {
                // Validate current user
                if (string.IsNullOrWhiteSpace(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var userId))
                {
                    return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
                }

                // Enforce role at handler level (defense in depth)
                if (string.IsNullOrWhiteSpace(_currentUserService.Role) ||
                    !string.Equals(_currentUserService.Role, UserRole.Student.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.NoPermission), ResultErrorType.Forbidden);
                }

                // Load student with terms and applications
                var student = await _unitOfWork.Repository<Student>()
                    .Query()
                    .Include(s => s.StudentTerms)
                        .ThenInclude(st => st.Term)
                    .Include(s => s.JobApplications)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

                if (student == null)
                {
                    return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound), ResultErrorType.NotFound);
                }

                // Edge: student must have a CV
                if (string.IsNullOrWhiteSpace(student.CvUrl))
                {
                    return Result<ApplyJobResponse>.Failure("Bạn chưa có CV. Vui lòng upload CV trước khi ứng tuyển.", ResultErrorType.BadRequest);
                }

                // Placed check
                if (student.InternshipStatus == StudentStatus.INTERNSHIP_IN_PROGRESS || student.InternshipStatus == StudentStatus.COMPLETED)
                {
                    return Result<ApplyJobResponse>.Failure("Bạn đã có nơi thực tập", ResultErrorType.Forbidden);
                }

                // Load job with enterprise and enterprise users
                var job = await _unitOfWork.Repository<Job>()
                    .Query()
                    .Include(j => j.Enterprise)
                        .ThenInclude(e => e.EnterpriseUsers)
                            .ThenInclude(eu => eu.User)
                    .Include(j => j.JobApplications)
                    .FirstOrDefaultAsync(j => j.JobId == request.JobId, cancellationToken);

                if (job == null)
                {
                    return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.NotFound), ResultErrorType.NotFound);
                }

                // Job status / deadline checks
                var now = DateTime.UtcNow;
                if (job.Status != JobStatus.OPEN)
                {
                    return Result<ApplyJobResponse>.Failure("Vị trí đã đóng", ResultErrorType.BadRequest);
                }

                if (job.ExpireDate.HasValue && job.ExpireDate.Value < now)
                {
                    return Result<ApplyJobResponse>.Failure("Hạn nộp hồ sơ đã hết hạn", ResultErrorType.BadRequest);
                }

                // Active application check (student should not have any active application: Applied/Interview/Offered)
                var hasActiveApp = student.JobApplications.Any(a =>
                    a.Status == JobApplicationStatus.Applied ||
                    a.Status == JobApplicationStatus.Interview ||
                    a.Status == JobApplicationStatus.Offered);

                if (hasActiveApp)
                {
                    return Result<ApplyJobResponse>.Failure("Bạn đang có ứng tuyển đang hoạt động. Vui lòng chờ kết quả trước khi ứng tuyển tiếp.", ResultErrorType.Conflict);
                }

                // Determine active term (for re-apply per term)
                var activeTerm = student.StudentTerms
                    .Select(st => st.Term)
                    .FirstOrDefault(t => t.Status == TermStatus.Open);

                if (activeTerm == null)
                {
                    return Result<ApplyJobResponse>.Failure("Không có kỳ học hoạt động. Không thể ứng tuyển.", ResultErrorType.BadRequest);
                }

                // Re-apply limit per job per term (configurable; default 3)
                var reapplyLimit = _configuration.GetValue<int?>("Jobs:MaxReapplyPerJobPerTerm") ?? 3;

                var termStart = activeTerm.StartDate.ToDateTime(new TimeOnly(0, 0));
                var termEnd = activeTerm.EndDate.ToDateTime(new TimeOnly(23, 59, 59));

                var pastApplicationsForJobInTerm = student.JobApplications
                    .Count(a => a.JobId == job.JobId && a.AppliedAt >= termStart && a.AppliedAt <= termEnd);

                if (pastApplicationsForJobInTerm >= reapplyLimit)
                {
                    return Result<ApplyJobResponse>.Failure("Bạn đã đạt giới hạn ứng tuyển cho vị trí này.", ResultErrorType.Forbidden);
                }

                // Prepare CV snapshot info
                var cvSnapshotUrl = student.CvUrl;
                var cvSnapshotFileName = ExtractFileNameFromUrl(cvSnapshotUrl);

                // Create application
                var application = JobApplication.Create(job.JobId, student.StudentId, Guid.Empty, null, cvSnapshotUrl, cvSnapshotFileName);
                await _unitOfWork.Repository<JobApplication>().AddAsync(application, cancellationToken);
                await _unitOfWork.SaveChangeAsync(cancellationToken);

                // Enqueue background email to enterprise users (HR)
                var hrEmails = job.Enterprise?.EnterpriseUsers?
                    .Select(eu => eu.User?.Email)
                    .Where(e => !string.IsNullOrWhiteSpace(e))
                    .Distinct()
                    .ToList();

                if (hrEmails != null && hrEmails.Any())
                {
                    var subject = $"[IOCv2] Ứng tuyển mới: {job.Title}";
                    var body = $"Sinh viên vừa ứng tuyển vị trí \"{job.Title}\" tại {job.Enterprise?.Name}. Vui lòng đăng nhập để xem hồ sơ.\n\nStudent: {student.User?.FullName ?? "Unknown"}\nApplied at: {application.AppliedAt:u}";
                    foreach (var email in hrEmails)
                    {
                        try
                        {
                            await _backgroundEmailSender.EnqueueEmailAsync(email!, subject, body, application.ApplicationId, null, cancellationToken);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to enqueue application notification email to {Email}", email);
                        }
                    }
                }

                var response = new ApplyJobResponse { ApplicationId = application.ApplicationId };
                return Result<ApplyJobResponse>.Success(response, $"Apply thành công! Vui lòng chờ phản hồi từ {job.Enterprise?.Name ?? "Enterprise"}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying for job {JobId} by user {UserId}", request.JobId, _currentUserService.UserId);
                return Result<ApplyJobResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }

        private static string? ExtractFileNameFromUrl(string? url)
        {
            if (string.IsNullOrWhiteSpace(url)) return null;
            try
            {
                // Try to parse as Uri, fallback to last segment
                if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    return System.IO.Path.GetFileName(uri.LocalPath);
                }

                return url.Split('/').LastOrDefault();
            }
            catch
            {
                return null;
            }
        }
    }
}
