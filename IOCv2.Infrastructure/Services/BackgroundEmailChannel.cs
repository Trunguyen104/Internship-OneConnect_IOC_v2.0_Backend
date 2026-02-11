using IOCv2.Application.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace IOCv2.Infrastructure.Services
{
    /// <summary>
    /// Implements a simple in-memory queue for emails using System.Threading.Channels.
    /// This allows the application to fire-and-forget email sending tasks.
    /// </summary>
    public class BackgroundEmailChannel : IBackgroundEmailSender
    {
        private readonly Channel<EmailMessage> _channel;
        private readonly ILogger<BackgroundEmailChannel> _logger;

        public BackgroundEmailChannel(ILogger<BackgroundEmailChannel> logger)
        {
            _logger = logger;
            // Bounded channel to prevent memory overflow if email sending is very slow
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _channel = Channel.CreateBounded<EmailMessage>(options);
        }

        public async ValueTask EnqueueEmailAsync(string recipientEmail, string subject, string body, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken)
        {
            var message = new EmailMessage
            {
                To = recipientEmail,
                Subject = subject,
                Body = body,
                AuditTargetId = auditTargetId,
                PerformedByEmployeeId = performedByEmployeeId
            };

            await _channel.Writer.WriteAsync(message, cancellationToken);
            _logger.LogInformation("Email to {Email} queued for background delivery.", recipientEmail);
        }

        public ChannelReader<EmailMessage> Reader => _channel.Reader;

        public async ValueTask EnqueueAccountCreationEmailAsync(string email, string username, string role, string password, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetAccountCreationTemplate(username, role, password);
            await EnqueueEmailAsync(email, "Chào mừng đến Internship OneConnect - Thông tin tài khoản", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueueRoleChangeEmailAsync(string email, string oldUsername, string newUsername, string oldRole, string newRole, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetRoleChangeTemplate(oldUsername, newUsername, oldRole, newRole);
            await EnqueueEmailAsync(email, "Thông báo thay đổi vai trò - Internship OneConnect", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueuePasswordResetByManagerEmailAsync(string email, string username, string newPassword, string managerName, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetPasswordResetByManagerTemplate(username, newPassword, managerName);
            await EnqueueEmailAsync(email, "Mật khẩu của bạn đã được reset - Internship OneConnect", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueuePasswordResetEmailAsync(string email, string resetLink, string username, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetPasswordResetTemplate(username, resetLink);
            await EnqueueEmailAsync(email, "Password Reset - Internship OneConnect", body, auditTargetId, performedByEmployeeId, cancellationToken);
            await EnqueueEmailAsync(email, "Password Reset - Internship OneConnect", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }
    }
}
