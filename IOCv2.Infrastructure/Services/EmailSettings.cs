namespace IOCv2.Infrastructure.Services
{
    public class EmailSettings
    {
        // Gmail SMTP Settings
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string SenderEmail { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;

        // Gmail App Password (not your regular Gmail password!)
        // Generate at: https://myaccount.google.com/apppasswords
        public string AppPassword { get; set; } = string.Empty;
    }
}
