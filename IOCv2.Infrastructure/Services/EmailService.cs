using DnsClient;
using DnsClient.Protocol;
using IOCv2.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace IOCv2.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _logger = logger;
            _emailSettings = emailSettings.Value;

            if (string.IsNullOrWhiteSpace(_emailSettings.SenderEmail))
            {
                _logger.LogWarning("EmailService: SenderEmail is not configured. Email sending will fail.");
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(
            string email,
            string resetLink,
            string employeeName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000 // 30 seconds
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Password Reset - Internship OneConnect",
                    Body = EmailTemplates.GetPasswordResetTemplate(employeeName, resetLink),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Password reset email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                return false;
            }
        }

        public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(to)) throw new ArgumentNullException(nameof(to));
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentNullException(nameof(subject));
            if (string.IsNullOrWhiteSpace(body)) throw new ArgumentNullException(nameof(body));

            if (!IsValidEmail(to))
            {
                throw new ArgumentException("Invalid email format", nameof(to));
            }

            ct.ThrowIfCancellationRequested();

            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    EnableSsl = true
                };

                if (string.IsNullOrWhiteSpace(_emailSettings.SenderEmail))
                {
                    throw new InvalidOperationException("Cannot send email: SenderEmail configuration is missing.");
                }

                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(to);

                await smtpClient.SendMailAsync(mailMessage, ct);

                _logger.LogInformation("Email sent successfully to {Email}", to);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FAILED to send email to {Email}", to);
                throw;
            }
        }

        public async Task<bool> SendAccountCreationEmailAsync(
            string email,
            string fullName,
            string loginEmail,
            string role,
            string password,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Chào mừng đến Internship OneConnect - Thông tin tài khoản",
                    Body = EmailTemplates.GetAccountCreationTemplate(fullName, loginEmail, role, password),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Account creation email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send account creation email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendRoleChangeConfirmationEmailAsync(
            string email,
            string employeeName,
            string oldUserCode,
            string newUserCode,
            string oldRole,
            string newRole,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Thông báo thay đổi vai trò - Internship OneConnect",
                    Body = EmailTemplates.GetRoleChangeTemplate(employeeName, oldUserCode, newUserCode, oldRole, newRole),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Role change confirmation email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send role change confirmation email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetByManagerEmailAsync(
            string email,
            string fullName,
            string userCode,
            string newPassword,
            string managerName,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidEmail(email))
            {
                throw new ArgumentException("Invalid email format", nameof(email));
            }
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000
                };
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Mật khẩu của bạn đã được reset - Internship OneConnect",
                    Body = EmailTemplates.GetPasswordResetByManagerTemplate(fullName, userCode, newPassword, managerName),
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);
                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Password reset by manager email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset by manager email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendUniversityCreationEmailAsync(
            string email,
            string universityName,
            string universityCode,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidEmail(email)) return false;
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000
                };
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Chào mừng đối tác Trường Đại học - Internship OneConnect",
                    Body = EmailTemplates.GetUniversityCreationTemplate(universityName, universityCode),
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);
                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("University creation email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send university creation email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendEnterpriseCreationEmailAsync(
            string email,
            string enterpriseName,
            string taxCode,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidEmail(email)) return false;
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000
                };
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Chào mừng đối tác Doanh nghiệp - Internship OneConnect",
                    Body = EmailTemplates.GetEnterpriseCreationTemplate(enterpriseName, taxCode),
                    IsBodyHtml = true
                };
                mailMessage.To.Add(email);
                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Enterprise creation email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send enterprise creation email to {Email}", email);
                return false;
            }
        }

        public async Task<bool> SendLandingReservationEmailAsync(
            string partnerType,
            string partnerName,
            string email,
            string phone,
            string area,
            string hiringCount,
            string consultationDate,
            string selectedTime,
            string note,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, "IOCv2 Landing Page Notification"),
                    Subject = $"📢 [Landing Page] Yêu cầu từ {(partnerType == "University" ? "Trường" : "Doanh nghiệp")}: {partnerName}",
                    Body = EmailTemplates.GetLandingReservationTemplate(partnerType, partnerName, email, phone, area, hiringCount, consultationDate, selectedTime, note),
                    IsBodyHtml = true
                };

                // Send TO the admin (sender email)
                mailMessage.To.Add(_emailSettings.SenderEmail);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Landing reservation email sent successfully to admin.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send landing reservation email for {PartnerName}", partnerName);
                return false;
            }
        }

        public async Task<bool> SendVerificationOtpEmailAsync(
            string email,
            string otpCode,
            CancellationToken cancellationToken = default)
        {
            if (!IsValidEmail(email) || string.IsNullOrWhiteSpace(otpCode))
            {
                return false;
            }

            try
            {
                using var smtpClient = new SmtpClient(_emailSettings.SmtpHost, _emailSettings.SmtpPort)
                {
                    EnableSsl = true,
                    Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.AppPassword),
                    Timeout = 30000
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                    Subject = "Mã xác thực email - Internship OneConnect",
                    Body = EmailTemplates.GetVerificationOtpTemplate(otpCode),
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
                _logger.LogInformation("Verification OTP email sent successfully to {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification OTP email to {Email}", email);
                return false;
            }
        }

        public bool VerifyEmailMxRecordSync(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
            {
                return false;
            }

            var at = email.LastIndexOf('@');
            if (at <= 0 || at == email.Length - 1)
            {
                return false;
            }

            var domain = email[(at + 1)..].Trim();
            if (string.IsNullOrEmpty(domain))
            {
                return false;
            }

            try
            {
                var lookup = new LookupClient();
                var result = lookup.Query(domain, QueryType.MX);
                return result.Answers.OfType<MxRecord>().Any();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MX lookup failed for domain {Domain}", domain);
                return false;
            }
        }

        private static bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
