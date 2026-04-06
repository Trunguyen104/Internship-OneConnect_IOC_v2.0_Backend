using System;
using IOCv2.Infrastructure.Services;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IOCv2.Infrastructure.BackgroundJobs
{
    /// <summary>
    /// Daily job that closes expired published Job postings and notifies HR + students with active applications.
    /// </summary>
    public class JobExpiryHostedService : BackgroundService
    {
        private readonly ILogger<JobExpiryHostedService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public JobExpiryHostedService(IServiceProvider serviceProvider, ILogger<JobExpiryHostedService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("JobExpiryHostedService starting.");

            // Run once immediately on start, then every 24 hours.
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        await ProcessExpiredJobsAsync(stoppingToken);
                    }
                    catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                    {
                        // graceful shutdown
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unhandled error while processing expired jobs.");
                    }

                    // Wait 24 hours or until cancellation
                    await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
                }
            }
            finally
            {
                _logger.LogInformation("JobExpiryHostedService stopping.");
            }
        }

        private async Task ProcessExpiredJobsAsync(CancellationToken ct)
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var emailChannel = scope.ServiceProvider.GetRequiredService<IBackgroundEmailSender>();

            // Query for published jobs whose expire date is before today (UTC date)
            var jobRepo = unitOfWork.Repository<Job>();
            var today = DateTime.UtcNow.Date;
            
            // Efficiently fetch expired jobs with necessary relations
            var expiredJobs = await jobRepo.Query()
                .Include(j => j.Enterprise)
                    .ThenInclude(e => e.EnterpriseUsers)
                        .ThenInclude(eu => eu.User)
                .Include(j => j.InternshipApplications)
                    .ThenInclude(ia => ia.Student)
                        .ThenInclude(s => s.User)
                .Where(j => j.Status == JobStatus.PUBLISHED 
                            && j.ExpireDate.HasValue 
                            && j.ExpireDate.Value.Date < today)
                .ToListAsync(ct);

            if (expiredJobs.Count == 0)
            {
                _logger.LogInformation("No expired published jobs found at {Time}.", DateTime.UtcNow);
                return;
            }

            _logger.LogInformation("Found {Count} expired published job(s) to close.", expiredJobs.Count);

            foreach (var job in expiredJobs)
            {
                try
                {
                    // Close job within transaction
                    await unitOfWork.BeginTransactionAsync(ct);
                    job.Status = JobStatus.CLOSED;
                    job.UpdatedAt = DateTime.UtcNow;

                    await jobRepo.UpdateAsync(job, ct);
                    await unitOfWork.SaveChangeAsync(ct);
                    await unitOfWork.CommitTransactionAsync(ct);

                    _logger.LogInformation("Job {JobId} closed automatically (expire date {ExpireDate}).", job.JobId, job.ExpireDate);

                    // Notify HR(s) for the enterprise
                    try
                    {
                        var enterpriseUserEmails = job.Enterprise?.EnterpriseUsers?
                            .Select(eu => eu.User?.Email)
                            .Where(email => !string.IsNullOrWhiteSpace(email))
                            .Distinct()
                            .ToList() ?? new();

                        if (enterpriseUserEmails.Any())
                        {
                            var hrSubject = $"[IOC] Tuyển dụng [{job.Title}] đã tự động đóng do hết hạn";
                            var hrBody = EmailTemplates.GetJobClosedNotificationTemplate(job.Title, job.Enterprise?.Name ?? "Doanh nghiệp", job.ExpireDate?.ToString("yyyy-MM-dd") ?? "-", false);

                            foreach (var hrEmail in enterpriseUserEmails)
                            {
                                try
                                {
                                    await emailChannel.EnqueueEmailAsync(hrEmail!, hrSubject, hrBody, job.JobId, null, ct);
                                    _logger.LogInformation("Enqueued HR notification for {Email} about job {JobId}.", hrEmail, job.JobId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to enqueue HR email for {Email} (job {JobId}).", hrEmail, job.JobId);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to resolve/notify enterprise HRs for job {JobId}.", job.JobId);
                    }

                    // Notify students who have Active applications for this job
                    try
                    {
                        var activeStatuses = new[]
                        {
                            InternshipApplicationStatus.Applied,
                            InternshipApplicationStatus.Interviewing,
                            InternshipApplicationStatus.Offered,
                            InternshipApplicationStatus.PendingAssignment
                        };

                        var studentEmails = job.InternshipApplications?
                            .Where(a => activeStatuses.Contains(a.Status))
                            .Select(a => a.Student?.User?.Email)
                            .Where(email => !string.IsNullOrWhiteSpace(email))
                            .Distinct()
                            .ToList() ?? new();

                        if (studentEmails.Any())
                        {
                            var studentSubject = $"[IOC] Tuyển dụng [{job.Title}] đã đóng do hết hạn";
                            var studentBody = EmailTemplates.GetJobClosedNotificationTemplate(job.Title, job.Enterprise?.Name ?? "Doanh nghiệp", job.ExpireDate?.ToString("yyyy-MM-dd") ?? "-", true);

                            foreach (var studentEmail in studentEmails)
                            {
                                try
                                {
                                    await emailChannel.EnqueueEmailAsync(studentEmail!, studentSubject, studentBody, job.JobId, null, ct);
                                    _logger.LogInformation("Enqueued student notification for {Email} about job {JobId}.", studentEmail, job.JobId);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Failed to enqueue student email for {Email} (job {JobId}).", studentEmail, job.JobId);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to resolve/notify students for job {JobId}.", job.JobId);
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error while closing job {JobId}. Rolling back.", job.JobId);
                    await unitOfWork.RollbackTransactionAsync(ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error while processing expired job {JobId}. Rolling back.", job.JobId);
                    try { await unitOfWork.RollbackTransactionAsync(ct); } catch { /* ignore rollback failure */ }
                }
            }
        }
    }
}
