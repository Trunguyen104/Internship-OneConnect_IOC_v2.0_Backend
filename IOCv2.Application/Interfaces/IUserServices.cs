using IOCv2.Domain.Enums;

namespace IOCv2.Application.Interfaces
{
    public interface IUserServices
    {
        public Task<string> GenerateUserCodeAsync(UserRole role);
    }
}
