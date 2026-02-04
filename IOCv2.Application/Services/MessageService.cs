using IOCv2.Application.Interfaces;
using System.Globalization;
using System.Text;

namespace IOCv2.Application.Services
{
    public class MessageService : IMessageService
    {
        public string GetMessage(string key)
        {
            // Try to get from Messages first, then ErrorMessages
            var message = Resources.Messages.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);

            if (string.IsNullOrEmpty(message))
            {
                message = Resources.ErrorMessages.ResourceManager.GetString(key, CultureInfo.CurrentUICulture);
            }

            return message ?? key; // Fallback to key if not found
        }

        public string GetMessage(string key, params object[] args)
        {
            var message = GetMessage(key);
            return string.Format(message, args);
        }
    }
}
