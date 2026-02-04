using IOCv2.Domain.Entities;

namespace IOCv2.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        string GenerateRefreshToken();
        int GetTokenExpirationSeconds();
        int GetRefreshTokenExpirationDays();
    }
}
