using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Infrastructure.Services
{
    public class LandingEmailPolicy : ILandingEmailPolicy
    {
        private readonly IUnitOfWork _unitOfWork;

        public LandingEmailPolicy(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> IsRegisteredEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            var normalized = email.Trim().ToLowerInvariant();

            // Kiểm tra xem email có tồn tại trong hệ thống User không
            return await _unitOfWork.Repository<User>()
                .Query()
                .AnyAsync(u => u.Email.ToLower() == normalized, cancellationToken);
        }
    }
}
