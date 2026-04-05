namespace IOCv2.Infrastructure.Services
{
    public static partial class EmailTemplates
    {
        public static string GetUniversityCreationTemplate(string universityName, string universityCode)
        {
            var content = $@"
                <p>Xin chào đối tác Trường Đại học,</p>
                <p>Trường <strong>{universityName}</strong> đã được tích hợp thành công vào hệ thống <strong>Internship OneConnect</strong>.</p>
                
                <div class='info-box'>
                    <div class='info-row'>
                        <div class='info-label'>Tên trường:</div>
                        <div class='info-value'>{universityName}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Mã hiệu cơ sở:</div>
                        <div class='info-value'>{universityCode}</div>
                    </div>
                </div>
                
                <p>Thông tin của trường hiện đang được Quản trị viên xử lý tiếp các bước tích hợp. Bạn sẽ nhận được thông tin tài khoản ngay khi quá trình hoàn tất.</p>
                <p>Vui lòng liên hệ với đội ngũ hỗ trợ IOC nếu bạn có thắc mắc.</p>";

            return BaseLayout("🎓 Chào mừng Đối tác Trường Đại học", content);
        }

        public static string GetEnterpriseCreationTemplate(string enterpriseName, string taxCode)
        {
            var content = $@"
                <p>Xin chào quý đối tác Doanh nghiệp,</p>
                <p>Doanh nghiệp <strong>{enterpriseName}</strong> đã được đăng ký thành công trên nền tảng <strong>Internship OneConnect</strong>.</p>
                
                <div class='info-box'>
                    <div class='info-row'>
                        <div class='info-label'>Tên doanh nghiệp:</div>
                        <div class='info-value'>{enterpriseName}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Mã số thuế:</div>
                        <div class='info-value'>{taxCode}</div>
                    </div>
                </div>
                
                <p>Hệ thống sẽ tiến hành phê duyệt hồ sơ doanh nghiệp trong thời gian sớm nhất. Bạn sẽ nhận được email xác nhận tài khoản ngay sau khi được phê duyệt.</p>
                <p>Chào mừng bạn gia nhập cộng đồng kết nối thực tập OneConnect.</p>";

            return BaseLayout("🏢 Chào mừng Đối tác Doanh nghiệp", content);
        }
    }
}
