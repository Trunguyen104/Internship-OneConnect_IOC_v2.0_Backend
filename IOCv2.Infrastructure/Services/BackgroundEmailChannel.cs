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

        public async ValueTask EnqueueAccountCreationEmailAsync(string email, string fullname, string loginEmail, string role, string password, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetAccountCreationTemplate(fullname, loginEmail, role, password);
            await EnqueueEmailAsync(email, "Chào mừng đến Internship OneConnect - Thông tin tài khoản", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueueRoleChangeEmailAsync(string email, string oldUserCode, string newUserCode, string oldRole, string newRole, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetRoleChangeTemplate(email, oldUserCode, newUserCode, oldRole, newRole);
            await EnqueueEmailAsync(email, "Thông báo thay đổi vai trò - Internship OneConnect", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueuePasswordResetBySuperAdminEmailAsync(string email, string fullname, string newPassword, string superAdminName, Guid? auditTargetId, Guid? performedByEmployeeId, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetPasswordResetByManagerTemplate(fullname, email, newPassword, superAdminName);
            await EnqueueEmailAsync(email, "Mật khẩu của bạn đã được reset - Internship OneConnect", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueuePasswordResetEmailAsync(string email, string resetLink, string fullname, Guid? auditTargetId = null, Guid? performedByEmployeeId = null, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetPasswordResetTemplate(fullname, resetLink);
            await EnqueueEmailAsync(email, "Password Reset - Internship OneConnect", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueueUniversityCreationEmailAsync(string email, string universityName, string universityCode, Guid? auditTargetId = null, Guid? performedByEmployeeId = null, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetUniversityCreationTemplate(universityName, universityCode);
            await EnqueueEmailAsync(email, "Chào mừng đối tác Trường Đại học - Internship OneConnect", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }

        public async ValueTask EnqueueEnterpriseCreationEmailAsync(string email, string enterpriseName, string taxCode, Guid? auditTargetId = null, Guid? performedByEmployeeId = null, CancellationToken cancellationToken = default)
        {
            var body = EmailTemplates.GetEnterpriseCreationTemplate(enterpriseName, taxCode);
            await EnqueueEmailAsync(email, "Chào mừng đối tác Doanh nghiệp - Internship OneConnect", body, auditTargetId, performedByEmployeeId, cancellationToken);
        }
    }
}
