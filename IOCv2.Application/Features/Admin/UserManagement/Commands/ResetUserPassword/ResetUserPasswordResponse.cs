using IOCv2.Application.Extensions.Mappings;

namespace IOCv2.Application.Features.Admin.UserManagement.Commands.ResetUserPassword
{
    public class ResetUserPasswordResponse
    {
        public Guid UserId { get; set; }
        public string UserCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime ResetAt { get; set; }
        public string Message { get; set; } = string.Empty;

    }
}