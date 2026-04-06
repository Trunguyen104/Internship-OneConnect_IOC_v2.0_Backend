namespace IOCv2.Infrastructure.Services
{
    public static partial class EmailTemplates
    {
        /// <summary>
        /// Standardizes the base layout of all emails to ensure consistency in style and branding.
        /// </summary>
        private static string BaseLayout(string title, string htmlBody)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; background-color: #f4f4f4; }}
        .container {{ max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
        .header {{ background-color: #ab1f24; color: #ffffff; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; font-weight: 600; }}
        .content {{ padding: 30px; }}
        .content p {{ margin-bottom: 20px; }}
        .info-box {{ background: #f9f9f9; border-left: 4px solid #ab1f24; padding: 20px; margin: 20px 0; border-radius: 4px; }}
        .info-row {{ margin-bottom: 12px; }}
        .info-row:last-child {{ margin-bottom: 0; }}
        .info-label {{ color: #666; font-size: 13px; text-transform: uppercase; letter-spacing: 0.5px; }}
        .info-value {{ color: #111; font-weight: bold; font-size: 16px; margin-top: 4px; }}
        .button {{ display: inline-block; padding: 12px 30px; background-color: #ab1f24; color: #ffffff !important; text-decoration: none; border-radius: 5px; font-weight: bold; margin: 20px 0; }}
        .warning {{ background-color: #fff8e1; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .warning strong {{ color: #856404; }}
        .warning ul {{ margin: 10px 0 0 20px; padding: 0; color: #856404; }}
        .footer {{ text-align: center; padding: 20px; color: #888; font-size: 12px; background-color: #f9f9f9; }}
        .strikethrough {{ text-decoration: line-through; color: #999; }}
        .box-old {{ background: #fff5f5; border: 1px solid #feb2b2; padding: 15px; border-radius: 4px; margin-bottom: 15px; }}
        .box-new {{ background: #f0fff4; border: 1px solid #9ae6b4; padding: 15px; border-radius: 4px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>{title}</h1>
        </div>
        <div class='content'>
            {htmlBody}
            <p>Trân trọng,<br><strong>Đội ngũ IOC System</strong></p>
        </div>
        <div class='footer'>
            <p>Đây là email tự động. Vui lòng không trả lời email này.</p>
            <p>&copy; {System.DateTime.Now.Year} Internship OneConnect. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
