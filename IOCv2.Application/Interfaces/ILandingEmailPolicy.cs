using System.Threading;
using System.Threading.Tasks;

namespace IOCv2.Application.Interfaces
{
    public interface ILandingEmailPolicy
    {
        Task<bool> IsRegisteredEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}
