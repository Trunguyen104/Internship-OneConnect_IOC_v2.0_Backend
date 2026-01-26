using IOC.Application.Commons.Interfaces.Services;
using Microsoft.AspNetCore.Identity;

namespace IOC.Infrastructure.Services
{
    // Implementation of IPasswordHasher using ASP.NET Core Identity's PasswordHasher<T>
    public class PasswordHasher : IPasswordHasher
    {
        private readonly PasswordHasher<object> _hasher = new();

        // Create a hash from a plain-text password
        public string Hash(string password)
        {
            return _hasher.HashPassword(null, password);
        }

        // Verify a plain-text password against a stored hash
        public bool Verify(string hashedPassword, string providedPassword)
        {
            if (string.IsNullOrWhiteSpace(hashedPassword) || string.IsNullOrEmpty(providedPassword))
                return false;

            var result = _hasher.VerifyHashedPassword(null, hashedPassword, providedPassword);
            return result != PasswordVerificationResult.Failed;
        }
    }

}