using IOC.Application.Auth.DTOs;
using IOC.Domain.Entities;

namespace IOC.Application.Commons.Interfaces.Services
{
    // Token generation contract - returns token details for an authenticated account
    public interface ITokenService
    {
        LoginResultDto GenerateToken(AdminAccount account);
    }
}