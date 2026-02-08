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

        public async ValueTask EnqueueAccountCreationEmailAsync(string email, string employeeName, string employeeCode, string role, string password, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetAccountCreationTemplate(employeeName, employeeCode, role, password);
            await EnqueueEmailAsync(email, "Chào mừng đến FoodHub - Thông tin tài khoản", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueueRoleChangeEmailAsync(string email, string employeeName, string oldEmployeeCode, string newEmployeeCode, string oldRole, string newRole, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetRoleChangeTemplate(employeeName, oldEmployeeCode, newEmployeeCode, oldRole, newRole);
            await EnqueueEmailAsync(email, "Thông báo thay đổi vai trò - FoodHub", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueuePasswordResetByManagerEmailAsync(string email, string employeeName, string employeeCode, string newPassword, string managerName, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetPasswordResetByManagerTemplate(employeeName, employeeCode, newPassword, managerName);
            await EnqueueEmailAsync(email, "Mật khẩu của bạn đã được reset - FoodHub", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueuePasswordResetEmailAsync(string email, string resetLink, string employeeName, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetPasswordResetTemplate(employeeName, resetLink);
            await EnqueueEmailAsync(email, "Password Reset - FoodHub", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }
    }
}
