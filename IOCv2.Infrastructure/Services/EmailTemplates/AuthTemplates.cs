namespace IOCv2.Infrastructure.Services
{
    public static partial class EmailTemplates
    {
        public static string GetPasswordResetTemplate(string fullName, string resetLink)
        {
            var content = $@"
                <p>Xin chào <strong>{fullName}</strong>,</p>
                <p>Chúng tôi nhận được yêu cầu cài lại mật khẩu cho tài khoản <strong>Internship OneConnect</strong> của bạn.</p>
                <p>Để tiếp tục, vui lòng nhấp vào nút bên dưới để đổi mật khẩu mới:</p>
                
                <div style='text-align: center;'>
                    <a href='{resetLink}' class='button'>Reset Password</a>
                </div>
                
                <div class='warning'>
                    <strong>⚠️ Lưu ý quan trọng:</strong>
                    <ul>
                        <li>Liên kết này chỉ có hiệu lực trong vòng <strong>15 phút</strong>.</li>
                        <li>Chỉ có thể sử dụng liên kết này <strong>một lần duy nhất</strong>.</li>
                        <li>Nếu bạn không yêu cầu cài lại mật khẩu, vui lòng bỏ qua email này.</li>
                    </ul>
                </div>";

            return BaseLayout("🔐 Reset Your Password", content);
        }

        public static string GetPasswordResetByManagerTemplate(string fullName, string userCode, string newPassword, string managerName)
        {
            var content = $@"
                <p>Xin chào <strong>{fullName}</strong>,</p>
                <p>Mật khẩu của bạn đã được Quản trị viên <strong>{managerName}</strong> cài lại trong hệ thống <strong>Internship OneConnect</strong>.</p>
                
                <div class='info-box'>
                    <div class='info-row'>
                        <div class='info-label'>Tên đăng nhập (Username):</div>
                        <div class='info-value'>{userCode}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Mật khẩu mới:</div>
                        <div class='info-value'>{newPassword}</div>
                    </div>
                </div>
                
                <div class='warning'>
                    <strong>⚠️ QUAN TRỌNG - Bạn cần làm ngay:</strong>
                    <ul>
                        <li>Vui lòng <strong>đổi sang mật khẩu cá nhân</strong> ngay khi đăng nhập thành công.</li>
                        <li>Không chia sẻ mật khẩu tạm thời này với bất kỳ ai khác.</li>
                        <li>Sử dụng đúng tên đăng nhập ({userCode}) để truy cập hệ thống.</li>
                    </ul>
                </div>";

            return BaseLayout("🔐 Mật khẩu đã được reset", content);
        }
    }
}
