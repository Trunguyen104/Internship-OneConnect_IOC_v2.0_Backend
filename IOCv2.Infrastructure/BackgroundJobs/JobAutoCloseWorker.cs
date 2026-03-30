using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Background worker that runs daily to auto-close expired published jobs.
    /// Registers as a hosted service (AddHostedService&lt;JobAutoCloseWorker&gt;).
    /// </summary>
    public class JobAutoCloseWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobAutoCloseWorker> _logger;
        private readonly TimeSpan _runEvery = TimeSpan.FromDays(1);

        public JobAutoCloseWorker(IServiceProvider serviceProvider, ILogger<JobAutoCloseWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("JobAutoCloseWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error running JobAutoCloseWorker iteration.");
                }

                // Delay until next run (simple: run every 24 hours)
                await Task.Delay(_runEvery, stoppingToken);
            }

            _logger.LogInformation("JobAutoCloseWorker stopped.");
        }

        private async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var emailSender = scope.ServiceProvider.GetRequiredService<IBackgroundEmailSender>();

            var repo = unitOfWork.Repository<Job>();

            // Jobs published with deadline < today
            var today = DateTime.UtcNow.Date;
            var expiredJobs = await repo.Query()
                .Include(j => j.Enterprise).ThenInclude(e => e.EnterpriseUsers).ThenInclude(eu => eu.User)
                .Include(j => j.InternshipApplications).ThenInclude(ia => ia.Student).ThenInclude(s => s.User)
                .Where(j => j.Status == JobStatus.PUBLISHED && j.ExpireDate.HasValue && j.ExpireDate.Value.Date < today)
                .ToListAsync(cancellationToken);

            foreach (var job in expiredJobs)
            {
                try
                {
                    job.Status = JobStatus.CLOSED;
                    job.UpdatedAt = DateTime.UtcNow;

                    await repo.UpdateAsync(job, cancellationToken);
                    await unitOfWork.SaveChangeAsync(cancellationToken);

                    // Notify HR (all enterprise users)
                    var hrEmails = job.Enterprise?.EnterpriseUsers?
                        .Select(eu => eu.User?.Email)
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Distinct()
                        .ToList() ?? new();

                    var hrSubject = $"Job Posting \"{job.Title}\" đã tự động đóng do hết hạn";
                    var hrBody = $"Job Posting \"{job.Title}\" đã tự động đóng do hết hạn ({job.ExpireDate?.ToString("yyyy-MM-dd")}).";

                    foreach (var hrEmail in hrEmails)
                    {
                        _ = emailSender.EnqueueEmailAsync(hrEmail!, hrSubject, hrBody, job.JobId, null, cancellationToken);
                    }

                    // Notify students in active application statuses
                    var activeStatuses = new[] { InternshipApplicationStatus.Applied, InternshipApplicationStatus.Interviewing, InternshipApplicationStatus.Offered };
                    var recipients = job.InternshipApplications?
                        .Where(a => activeStatuses.Contains(a.Status))
                        .Select(a => a.Student?.User?.Email)
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Distinct()
                        .ToList() ?? new();

                    var studentSubject = $"Job Posting \"{job.Title}\" đã được đóng";
                    var studentBody = $"Job Posting \"{job.Title}\" đã được đóng. Hồ sơ của bạn vẫn sẽ được {job.Enterprise?.Name} tiếp tục xem xét nếu bạn đang trong quá trình phỏng vấn/offer.";

                    foreach (var studentEmail in recipients)
                    {
                        _ = emailSender.EnqueueEmailAsync(studentEmail!, studentSubject, studentBody, job.JobId, null, cancellationToken);
                    }

                    _logger.LogInformation("Auto-closed job {JobId} and notified HR and students.", job.JobId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to auto-close job {JobId}", job.JobId);
                }
            }
        }
    }
}
