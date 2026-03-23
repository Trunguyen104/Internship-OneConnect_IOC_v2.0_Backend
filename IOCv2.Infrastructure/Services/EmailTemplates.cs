namespace IOCv2.Infrastructure.Services
{
    public static class EmailTemplates
    {
        public static string GetPasswordResetTemplate(string fullName, string resetLink)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #ab1f24 0%, #8a1820 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
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
            <p>Hello <strong>" + fullName + @"</strong>,</p>
            
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

        public static string GetAccountCreationTemplate(string fullName, string email, string role, string password)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #ab1f24 0%, #8a1820 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
        .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }
        .info-box { background: white; border-left: 4px solid #ab1f24; padding: 15px; margin: 20px 0; border-radius: 4px; }
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
            <p>Xin chào <strong>" + fullName + @"</strong>,</p>
            
            <p>Tài khoản của bạn đã được tạo thành công trong hệ thống IOC. Dưới đây là thông tin đăng nhập của bạn:</p>
            
            <div class='info-box'>
                <div class='info-row'>
                    <div class='info-label'>Email đăng nhập:</div>
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

        public static string GetRoleChangeTemplate(string fullName, string oldUserCode, string newUserCode, string oldRole, string newRole)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #ab1f24 0%, #8a1820 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
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
            <p>Xin chào <strong>" + fullName + @"</strong>,</p>
            
            <p>Vai trò của bạn trong hệ thống IOC đã được thay đổi. Vui lòng xem thông tin chi tiết bên dưới:</p>
            
            <div class='change-box'>
                <div class='old-info'>
                    <h3 style='margin-top: 0; color: #f44336;'>❌ Thông tin cũ (đã vô hiệu hóa)</h3>
                    <div class='info-row'>
                        <div class='info-label'>Mã nhân viên cũ:</div>
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
                        <div class='info-label'>Mã nhân viên mới:</div>
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

        public static string GetPasswordResetByManagerTemplate(string fullName, string email, string newPassword, string managerName)
        {
            return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: linear-gradient(135deg, #ab1f24 0%, #8a1820 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }
        .content { background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }
        .info-box { background: white; border-left: 4px solid #ab1f24; padding: 15px; margin: 20px 0; border-radius: 4px; }
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
                    <div class='info-label'>Email đăng nhập:</div>
                    <div class='info-value'>" + email + @"</div>
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