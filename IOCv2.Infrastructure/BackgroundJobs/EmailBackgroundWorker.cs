using IOCv2.Application.Interfaces;
using IOCv2.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IOCv2.Infrastructure.BackgroundJobs
{
    public class EmailBackgroundWorker : BackgroundService
    {
        private readonly BackgroundEmailChannel _channel;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailBackgroundWorker> _logger;

        public EmailBackgroundWorker(
            BackgroundEmailChannel channel,
            IServiceProvider serviceProvider,
            ILogger<EmailBackgroundWorker> logger)
        {
            _channel = channel;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("EmailBackgroundWorker started.");

            await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await ProcessEmailAsync(message, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email for {Email}", message.To);
                }
            }

            _logger.LogInformation("EmailBackgroundWorker stopped.");
        }

        private async Task ProcessEmailAsync(EmailMessage message, CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            // Retry policy
            int maxRetries = 3;
            int delay = 1000;
            bool success = false;
            Exception? lastException = null;

            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    await emailService.SendEmailAsync(message.To, message.Subject, message.Body, stoppingToken);
                    success = true;
                    break;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogWarning("Attempt {Attempt} failed to send email to {Email}: {Message}", i + 1, message.To, ex.Message);
                    await Task.Delay(delay, stoppingToken);
                    delay *= 2; // Exponential backoff
                }
            }

            if (!success)
            {
                _logger.LogError(lastException, "FAILED to send email to {Email} after {Retries} attempts.", message.To, maxRetries);

                //if (message.AuditTargetId.HasValue && message.PerformedByEmployeeId.HasValue)
                //{
                //    await LogFailureToAudit(unitOfWork, message, lastException?.Message ?? "Unknown error");
                //}
            }
        }

        //private async Task LogFailureToAudit(IUnitOfWork unitOfWork, EmailMessage message, string errorDetails)
        //{
        //    try
        //    {
        //        var auditLog = new AuditLog
        //        {
        //            LogId = Guid.NewGuid(),
        //            Action = AuditAction.EmailFailure,
        //            TargetId = message.AuditTargetId!.Value,
        //            PerformedByEmployeeId = message.PerformedByEmployeeId!.Value,
        //            CreatedAt = DateTimeOffset.UtcNow,
        //            Reason = "Email Delivery Failed",
        //            Metadata = $"To: {message.To}, Subject: {message.Subject}, Error: {errorDetails}"
        //        };

        //        await unitOfWork.Repository<AuditLog>().AddAsync(auditLog);
        //        await unitOfWork.SaveChangeAsync();

        //        _logger.LogInformation("Logged failure to AuditLog for target {Target}", message.AuditTargetId);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "CRITICAL: Failed to log usage failure to AuditLog!");
        //    }
        //}
    }
}
