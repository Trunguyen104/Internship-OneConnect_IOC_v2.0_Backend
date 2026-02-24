using IOCv2.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IOCv2.Infrastructure.Services
{
    public class EmailHostedService : BackgroundService
    {
        private readonly ILogger<EmailHostedService> _logger;
        private readonly BackgroundEmailChannel _mailChannel;
        private readonly IServiceProvider _serviceProvider;

        public EmailHostedService(
            ILogger<EmailHostedService> logger,
            BackgroundEmailChannel mailChannel,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _mailChannel = mailChannel;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Hosted Service is starting.");

            try
            {
                await foreach (var message in _mailChannel.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                            _logger.LogInformation("Sending email to {Recipient}...", message.To);

                            await emailService.SendEmailAsync(
                                message.To,
                                message.Subject,
                                message.Body,
                                stoppingToken);

                            _logger.LogInformation("Email sent successfully to {Recipient}.", message.To);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error occurred executing email task for {Recipient}.", message.To);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal error in Email Hosted Service.");
            }

            _logger.LogInformation("Email Hosted Service is stopping.");
        }
    }
}
