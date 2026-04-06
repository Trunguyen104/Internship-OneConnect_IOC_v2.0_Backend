namespace IOCv2.Infrastructure.Services
{
    public static partial class EmailTemplates
    {
        public static string GetJobClosedNotificationTemplate(string jobTitle, string enterpriseName, string expireDate, bool isForStudent)
        {
            string title = isForStudent ? "Thông báo về Job Posting" : "Thông báo Job Posting hết hạn";
            
            var content = $@"
                <p>Xin chào,</p>
                <p>Vị trí tuyển dụng <strong>{jobTitle}</strong> tại <strong>{enterpriseName}</strong> đã tự động đóng vì hết hạn ({expireDate}).</p>
                
                <div class='info-box'>
                    <div class='info-row'>
                        <div class='info-label'>Tên công việc:</div>
                        <div class='info-value'>{jobTitle}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Ngày hết hạn:</div>
                        <div class='info-value'>{expireDate}</div>
                    </div>
                </div>";

            if (isForStudent)
            {
                content += @"
                    <div class='warning'>
                        <strong>💡 Lời khuyên cho bạn:</strong>
                        <ul>
                            <li>Hồ sơ của bạn vẫn được xem xét nếu đang phỏng vấn/offer.</li>
                            <li>Vui lòng tìm kiếm cơ hội khác đang mở trên sàn thực tập.</li>
                            <li>Theo dõi hòm thư và app để nhận thông báo mới.</li>
                        </ul>
                    </div>";
            }
            else
            {
                content += @"
                    <div class='warning'>
                        <strong>⚠️ Ghi chú cho HR:</strong>
                        <ul>
                            <li>Job đã chuyển sang <strong>CLOSED</strong> và ẩn với sinh viên.</li>
                            <li>Bạn vẫn quản lý ứng viên đã ứng tuyển trong Dashboard.</li>
                            <li>Có thể tạo Job mới hoặc gia hạn nếu cần thêm nhân sự.</li>
                        </ul>
                    </div>";
            }

            return BaseLayout(title, content);
        }
    }
}
