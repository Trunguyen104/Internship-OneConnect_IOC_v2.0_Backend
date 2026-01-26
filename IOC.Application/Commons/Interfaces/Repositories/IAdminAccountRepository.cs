using IOC.Application.AdminFeatures.DTOs;
using IOC.Application.Commons.Models.Paging;
using IOC.Domain.Entities;
using IOC.Domain.Enums;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOC.Application.Commons.Interfaces.Repositories
{
    public interface IAdminAccountRepository
    {
        Task<bool> ExistsByEmailAsync(string email);
        Task AddAsync(AdminAccount account);
        Task<AdminAccount?> GetByIdAsync(Guid id);
        Task UpdateAsync(AdminAccount account);
        Task DeleteAsync(AdminAccount account);
        Task<PagedResult<AdminAccountListDto>> GetListAsync(
            AdminAccountFilter filter,
            CancellationToken ct);

        // Added: count accounts by role to enforce master existence rules
        Task<int> CountByRoleAsync(AdminRole role);

        // Added: get admin account by email (used by authentication)
        Task<AdminAccount?> GetByEmailAsync(string email);
    }

}
