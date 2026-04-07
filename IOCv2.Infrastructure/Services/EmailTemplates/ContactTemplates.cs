namespace IOCv2.Infrastructure.Services
{
    public static partial class EmailTemplates
    {
        public static string GetLandingReservationTemplate(
            string partnerType,
            string partnerName,
            string email,
            string phone,
            string area,
            string hiringCount,
            string consultationDate,
            string selectedTime,
            string note)
        {
            var content = $@"
                <p>Bạn nhận được một yêu cầu tư vấn mới từ landing page:</p>
                <div class='info-box'>
                    <div class='info-row'>
                        <div class='info-label'>Loại đối tác:</div>
                        <div class='info-value'>{ (partnerType == "University" ? "Trường Đại học/Cao đẳng" : "Doanh nghiệp/Công ty") }</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Tên đơn vị:</div>
                        <div class='info-value'>{partnerName}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Email liên hệ:</div>
                        <div class='info-value'>{email}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Số điện thoại:</div>
                        <div class='info-value'>{phone}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Khu vực:</div>
                        <div class='info-value'>{area}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Dự kiến tuyển sinh/tuyển dụng:</div>
                        <div class='info-value'>{hiringCount}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Ngày tư vấn:</div>
                        <div class='info-value'>{consultationDate}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Thời gian:</div>
                        <div class='info-value'>{selectedTime}</div>
                    </div>
                    <div class='info-row'>
                        <div class='info-label'>Ghi chú:</div>
                        <div class='info-value'>{note ?? "N/A"}</div>
                    </div>
                </div>
                <p>Vui lòng liên hệ lại khách hàng sớm nhất có thể.</p>";

            return BaseLayout("📢 [Landing Page] Yêu cầu tư vấn mới", content);
        }
    }
}
