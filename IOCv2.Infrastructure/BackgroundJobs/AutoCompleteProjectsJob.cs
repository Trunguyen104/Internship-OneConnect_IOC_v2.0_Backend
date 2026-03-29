using IOCv2.Application.Features.Projects.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IOCv2.Infrastructure.BackgroundJobs
{
    public class AutoCompleteProjectsJob : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AutoCompleteProjectsJob> _logger;
        private static readonly TimeSpan Period = TimeSpan.FromHours(24);

        public AutoCompleteProjectsJob(IServiceProvider serviceProvider, ILogger<AutoCompleteProjectsJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AutoCompleteProjectsJob started.");

            using var timer = new PeriodicTimer(Period);
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunAsync(stoppingToken);
            }
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("AutoCompleteProjectsJob: Running auto-complete check at {Time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var pushService = scope.ServiceProvider.GetRequiredService<INotificationPushService>();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            try
            {
                var today = DateTime.UtcNow.Date;

                // Query projects: Active + group ended
                var expiredProjects = await unitOfWork.Repository<Project>().Query()
                    .Include(p => p.InternshipGroup)
                    .Where(p => p.OperationalStatus == OperationalStatus.Active
                             && p.InternshipGroup != null
                             && p.InternshipGroup.EndDate.HasValue
                             && p.InternshipGroup.EndDate.Value.Date < today)
                    .ToListAsync(cancellationToken);

                if (!expiredProjects.Any())
                {
                    _logger.LogInformation("AutoCompleteProjectsJob: No projects to auto-complete.");
                    return;
                }

                _logger.LogInformation("AutoCompleteProjectsJob: Auto-completing {Count} projects.", expiredProjects.Count);

                foreach (var project in expiredProjects)
                {
                    try
                    {
                        project.SetOperationalStatus(OperationalStatus.Completed);
                        await unitOfWork.Repository<Project>().UpdateAsync(project, cancellationToken);
                        await unitOfWork.SaveChangeAsync(cancellationToken);

                        // Invalidate single project cache
                        await cacheService.RemoveAsync(ProjectCacheKeys.Project(project.ProjectId), cancellationToken);

                        // Notify Mentor
                        if (project.MentorId.HasValue)
                        {
                            var mentorEu = await unitOfWork.Repository<EnterpriseUser>().Query()
                                .AsNoTracking()
                                .FirstOrDefaultAsync(eu => eu.EnterpriseUserId == project.MentorId.Value, cancellationToken);

                            if (mentorEu != null)
                            {
                                var notification = new Notification
                                {
                                    NotificationId = Guid.NewGuid(),
                                    UserId = mentorEu.UserId,
                                    Title = "Dự án tự động hoàn thành",
                                    Content = $"Dự án {project.ProjectName} đã tự động hoàn thành.",
                                    Type = NotificationType.General,
                                    ReferenceType = "Project",
                                    ReferenceId = project.ProjectId,
                                    IsRead = false,
                                    CreatedAt = DateTime.UtcNow
                                };
                                await unitOfWork.Repository<Notification>().AddAsync(notification, cancellationToken);
                                await unitOfWork.SaveChangeAsync(cancellationToken);

                                var unreadCount = await unitOfWork.Repository<Notification>()
                                    .CountAsync(n => n.UserId == mentorEu.UserId && !n.IsRead, cancellationToken);

                                await pushService.PushNewNotificationAsync(mentorEu.UserId, new
                                {
                                    type = NotificationType.General,
                                    referenceType = "Project",
                                    referenceId = project.ProjectId,
                                    currentUnreadCount = unreadCount
                                }, cancellationToken);
                            }
                        }

                        _logger.LogInformation("AutoCompleteProjectsJob: Project {ProjectId} auto-completed.", project.ProjectId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "AutoCompleteProjectsJob: Error auto-completing project {ProjectId}.", project.ProjectId);
                    }
                }

                // Invalidate project list cache
                await cacheService.RemoveByPatternAsync(ProjectCacheKeys.ProjectListPattern(), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoCompleteProjectsJob: Unexpected error during auto-complete run.");
            }
        }
    }
}
