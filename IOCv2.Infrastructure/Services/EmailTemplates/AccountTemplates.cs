namespace IOCv2.Infrastructure.Services
{
    public static partial class EmailTemplates
    {
        public static string GetAccountCreationTemplate(string fullName, string email, string role, string password)
        {
            var content = $@"
                <p>Xin chào <strong>{fullName}</strong>,</p>
                <p>Chào mừng bạn gia nhập hệ thống <strong>Internship OneConnect</strong>. Dưới đây là thông tin tài khoản đăng nhập của bạn:</p>
                
                <div class='info-box'>
                    <div class='info-row'>
                        <div class='info-label'>Email đăng nhập:</div>
                        <div class='info-value'>{email}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Vai trò (Role):</div>
                        <div class='info-value'>{role}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Mật khẩu tạm thời:</div>
                        <div class='info-value'>{password}</div>
                    </div>
                </div>
                
                <div class='warning'>
                    <strong>⚠️ Quan trọng:</strong>
                    <ul>
                        <li>Vui lòng <strong>đổi mật khẩu mới ngay</strong> sau khi đăng nhập thành công.</li>
                        <li>Sử dụng đúng địa chỉ Email ({email}) làm tên đăng nhập.</li>
                        <li>Bảo mật thông tin đăng nhập và không chia sẻ cho bên thứ ba.</li>
                    </ul>
                </div>";

            return BaseLayout("🎉 Chào mừng đến với Internship OneConnect", content);
        }

        public static string GetRoleChangeTemplate(string fullName, string oldUserCode, string newUserCode, string oldRole, string newRole)
        {
            var content = $@"
                <p>Xin chào <strong>{fullName}</strong>,</p>
                <p>Vai trò của bạn trong hệ thống <strong>IOC</strong> đã được cập nhật. Vui lòng xem thông tin chi tiết dưới đây:</p>
                
                <div class='box-old' style='margin-bottom: 20px;'>
                    <h3 style='margin:0 0 10px 0; font-size:14px; color:#c53030;'>❌ Thông tin cũ (Đã vô hiệu hóa)</h3>
                    <div class='info-row'>
                        <div class='info-label'>Mã hiệu cũ:</div>
                        <div class='info-value strikethrough'>{oldUserCode}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Vai trò cũ:</div>
                        <div class='info-value strikethrough'>{oldRole}</div>
                    </div>
                </div>
                
                <div class='box-new'>
                    <h3 style='margin:0 0 10px 0; font-size:14px; color:#2f855a;'>✅ Thông tin mới (Đang hoạt động)</h3>
                    <div class='info-row'>
                        <div class='info-label'>Mã hiệu mới:</div>
                        <div class='info-value'>{newUserCode}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Vai trò mới:</div>
                        <div class='info-value'>{newRole}</div>
                    </div>
                </div>
                
                <div class='warning'>
                    <strong>⚠️ Lưu ý quan trọng:</strong>
                    <ul>
                        <li>Vui lòng sử dụng <strong>Mã hiệu mới ({newUserCode})</strong> để đăng nhập từ bây giờ.</li>
                        <li><strong>Mật khẩu không đổi</strong> - bạn vẫn dùng mật khẩu cũ.</li>
                        <li>Tài khoản gắn với mã hiệu cũ đã bị ngừng kích hoạt.</li>
                    </ul>
                </div>";

            return BaseLayout("🔄 Thông báo thay đổi vai trò", content);
        }
    }
}
