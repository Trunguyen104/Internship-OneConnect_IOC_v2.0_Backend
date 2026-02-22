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
            string userCode,
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
                    Body = EmailTemplates.GetAccountCreationTemplate(fullName, userCode, role, password),
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

        private string GetPasswordResetTemplate(string employeeName, string resetLink)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
        .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }
        .button { display: inline-block; padding: 12px 30px; background: #667eea; color: white; text-decoration: none; border-radius: 5px; margin: 20px 0; }
        .footer { text-align: center; margin-top: 20px; color: #666; font-size: 12px; }
        .warning { background: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 20px 0; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Reset Your Password</h1>
        </div>
        <div class='content'>
            <p>Hello <strong>" + employeeName + @"</strong>,</p>
            
            <p>We received a request to reset the password for your Internship OneConnect account.</p>
            
            <p>To reset your password, please click the button below:</p>
            
            <div style='text-align: center;'>
                <a href='" + resetLink + @"' class='button'>Reset Password</a>
            </div>
            
            <div class='warning'>
                <strong>⚠️ Important:</strong>
                <ul>
                    <li>This link is valid for <strong>15 minutes</strong></li>
                    <li>The link can be used <strong>only once</strong></li>
                    <li>If you did not request a password reset, please ignore this email</li>
                </ul>
            </div>
            
            <p>Best regards,<br><strong>IOC System</strong></p>
        </div>
        <div class='footer'>
            <p>This is an automated email. Please do not reply to this message.</p>
            <p>&copy; 2026 Internship OneConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetAccountCreationTemplate(string employeeName, string email, string role, string password)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
        .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }
        .info-box { background: white; border-left: 4px solid #667eea; padding: 15px; margin: 20px 0; border-radius: 4px; }
        .info-row { margin: 10px 0; }
        .info-label { color: #666; font-weight: normal; }
        .info-value { color: #333; font-weight: bold; font-size: 16px; }
        .warning { background: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 20px 0; }
        .footer { text-align: center; margin-top: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 Chào mừng đến với Internship OneConnect</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>" + employeeName + @"</strong>,</p>
            
            <p>Tài khoản của bạn đã được tạo thành công trong hệ thống IOC. Dưới đây là thông tin đăng nhập của bạn:</p>
            
            <div class='info-box'>
                <div class='info-row'>
                    <div class='info-label'>Tên đăng nhập (Username):</div>
                    <div class='info-value'>" + email + @"</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Vai trò (Role):</div>
                    <div class='info-value'>" + role + @"</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Mật khẩu tạm thời:</div>
                    <div class='info-value'>" + password + @"</div>
                </div>
            </div>
            
            <div class='warning'>
                <strong>⚠️ Quan trọng:</strong>
                <ul>
                    <li>Vui lòng <strong>đổi mật khẩu ngay</strong> khi đăng nhập lần đầu tiên</li>
                    <li>Không chia sẻ thông tin đăng nhập với bất kỳ ai</li>
                    <li>Sử dụng <strong>Email</strong> (" + email + @") để đăng nhập</li>
                </ul>
            </div>
            
            <p>Nếu bạn có bất kỳ câu hỏi nào, vui lòng liên hệ với quản lý của bạn.</p>
            
            <p>Chúc bạn làm việc hiệu quả!<br><strong>IOC System</strong></p>
        </div>
        <div class='footer'>
            <p>Đây là email tự động. Vui lòng không trả lời email này.</p>
            <p>&copy; 2026 Internship OneConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }

        private string GetRoleChangeTemplate(string employeeName, string oldUserCode, string newUserCode, string oldRole, string newRole)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
        .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }
        .change-box { background: white; padding: 20px; margin: 20px 0; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); }
        .old-info { background: #ffebee; padding: 15px; border-left: 4px solid #f44336; margin-bottom: 15px; border-radius: 4px; }
        .new-info { background: #e8f5e9; padding: 15px; border-left: 4px solid #4caf50; border-radius: 4px; }
        .info-row { margin: 8px 0; }
        .info-label { color: #666; font-size: 14px; }
        .info-value { color: #333; font-weight: bold; font-size: 16px; }
        .important { background: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 20px 0; }
        .footer { text-align: center; margin-top: 20px; color: #666; font-size: 12px; }
        .strikethrough { text-decoration: line-through; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔄 Thông báo thay đổi vai trò</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>" + employeeName + @"</strong>,</p>
            
            <p>Vai trò của bạn trong hệ thống IOC đã được thay đổi. Vui lòng xem thông tin chi tiết bên dưới:</p>
            
            <div class='change-box'>
                <div class='old-info'>
                    <h3 style='margin-top: 0; color: #f44336;'>❌ Thông tin cũ (đã vô hiệu hóa)</h3>
                    <div class='info-row'>
                        <div class='info-label'>Tên đăng nhập cũ:</div>
                        <div class='info-value strikethrough'>" + oldUserCode + @"</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Vai trò cũ:</div>
                        <div class='info-value strikethrough'>" + oldRole + @"</div>
                    </div>
                </div>
                
                <div class='new-info'>
                    <h3 style='margin-top: 0; color: #4caf50;'>✅ Thông tin mới (đang hoạt động)</h3>
                    <div class='info-row'>
                        <div class='info-label'>Tên đăng nhập mới:</div>
                        <div class='info-value'>" + newUserCode + @"</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Vai trò mới:</div>
                        <div class='info-value'>" + newRole + @"</div>
                    </div>
                </div>
            </div>
            
            <div class='important'>
                <strong>⚠️ Lưu ý quan trọng:</strong>
                <ul>
                    <li>Vui lòng sử dụng <strong>mã nhân viên mới</strong> (" + newUserCode + @") để đăng nhập</li>
                    <li><strong>Mật khẩu của bạn giữ nguyên</strong> - không thay đổi</li>
                    <li>Tài khoản cũ (" + oldUserCode + @") đã bị vô hiệu hóa và không thể đăng nhập</li>
                    <li>Quyền truy cập của bạn đã được cập nhật theo vai trò mới</li>
                </ul>
            </div>
            
            <p>Nếu bạn có bất kỳ thắc mắc nào về việc thay đổi này, vui lòng liên hệ với quản lý của bạn.</p>
            
            <p>Trân trọng,<br><strong>IOC System</strong></p>
        </div>
        <div class='footer'>
            <p>Đây là email tự động. Vui lòng không trả lời email này.</p>
            <p>&copy; 2026 Internship OneConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
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
        private string GetPasswordResetByManagerTemplate(string fullName, string userCode, string newPassword, string managerName)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
        .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }
        .info-box { background: white; border-left: 4px solid #f5576c; padding: 15px; margin: 20px 0; border-radius: 4px; }
        .info-row { margin: 10px 0; }
        .info-label { color: #666; font-size: 14px; }
        .info-value { color: #333; font-weight: bold; font-size: 16px; }
        .warning { background: #fff3cd; border-left: 4px solid #ffc107; padding: 10px; margin: 20px 0; }
        .footer { text-align: center; margin-top: 20px; color: #666; font-size: 12px; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🔐 Mật khẩu đã được reset</h1>
        </div>
        <div class='content'>
            <p>Xin chào <strong>" + fullName + @"</strong>,</p>
            
            <p>Mật khẩu của bạn đã được Manager <strong>" + managerName + @"</strong> reset trong hệ thống IOC.</p>
            
            <div class='info-box'>
                <div class='info-row'>
                    <div class='info-label'>Tên đăng nhập (Username):</div>
                    <div class='info-value'>" + userCode + @"</div>
                </div>
                <div class='info-row'>
                    <div class='info-label'>Mật khẩu mới:</div>
                    <div class='info-value'>" + newPassword + @"</div>
                </div>
            </div>
            
            <div class='warning'>
                <strong>⚠️ QUAN TRỌNG - Bạn cần làm ngay:</strong>
                <ul>
                    <li><strong>BẮT BUỘC phải đổi mật khẩu</strong> ngay khi đăng nhập lần đầu tiên</li>
                    <li>Không chia sẻ mật khẩu này với bất kỳ ai</li>
                    <li>Sử dụng tên đăng nhập (" + userCode + @") để đăng nhập</li>
                    <li>Chọn một mật khẩu mạnh mà chỉ bạn biết</li>
                </ul>
            </div>
            
            <p>Nếu bạn không yêu cầu reset mật khẩu, vui lòng liên hệ với Manager ngay lập tức.</p>
            
            <p>Trân trọng,<br><strong>IOC System</strong></p>
        </div>
        <div class='footer'>
            <p>Đây là email tự động. Vui lòng không trả lời email này.</p>
            <p>&copy; 2026 Internship OneConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
