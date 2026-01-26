using IOC.Application.AdminFeatures.DTOs;
using IOC.Application.Commons.Interfaces.Repositories;
using IOC.Application.Commons.Models.Paging;
using IOC.Domain.Entities;
using IOC.Domain.ValueObjects;
using IOC.Infrastructure.Persistences.DbContexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IOC.Infrastructure.Repositories
{
    public class AdminAccountRepository : IAdminAccountRepository
    {
        private readonly AppDbContext _context;

        public AdminAccountRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<bool> ExistsByEmailAsync(string email)
        {
            return await _context.AdminAccounts
                .AnyAsync(x => x.Email == Domain.ValueObjects.Email.Create(email));
        }

        public async Task AddAsync(AdminAccount account)
        {
            _context.AdminAccounts.Add(account);
            await _context.SaveChangesAsync();
        }

        public async Task<AdminAccount?> GetByIdAsync(Guid id)
        {
            return await _context.AdminAccounts
                .Include(a => a.Organization)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task UpdateAsync(AdminAccount account)
        {
            var entry = _context.Entry(account);
            if (entry.State == EntityState.Detached)
            {
                _context.AdminAccounts.Attach(account);
                entry.State = EntityState.Modified;
            }

            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(AdminAccount account)
        {
            _context.AdminAccounts.Remove(account);
            await _context.SaveChangesAsync();
        }

        public async Task<int> CountByRoleAsync(Domain.Enums.AdminRole role)
        {
            return await _context.AdminAccounts.CountAsync(x => x.Role == role);
        }

        // New: find admin account by email for authentication
        public async Task<AdminAccount?> GetByEmailAsync(string email)
        {
            var emailVo = Domain.ValueObjects.Email.Create(email);
            return await _context.AdminAccounts
                .Include(a => a.Organization)
                .FirstOrDefaultAsync(a => a.Email == emailVo);
        }

        public async Task<PagedResult<AdminAccountListDto>> GetListAsync(
        AdminAccountFilter filter,
        CancellationToken ct)
        {
            var query = _context.AdminAccounts.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.ToLower();
                query = query.Where(x =>
                    x.FullName.ToLower().Contains(keyword) ||
                    x.Email.Value.Contains(keyword) ||
                    x.Code.ToLower().Contains(keyword));
            }

            if (filter.Role.HasValue)
                query = query.Where(x => x.Role == filter.Role);

            if (filter.Status.HasValue)
                query = query.Where(x => x.Status == filter.Status);

            query = filter.SortBy switch
            {
                "FullName" => filter.SortDir == "desc"
                    ? query.OrderByDescending(x => x.FullName)
                    : query.OrderBy(x => x.FullName),

                "CreatedAt" => filter.SortDir == "desc"
                    ? query.OrderByDescending(x => x.CreatedAt)
                    : query.OrderBy(x => x.CreatedAt),

                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var total = await query.CountAsync(ct);

            var data = await query
                .Skip((filter.PageIndex - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(x => new AdminAccountListDto
                {
                    Id = x.Id,
                    Code = x.Code,
                    FullName = x.FullName,
                    Email = x.Email.Value,
                    OrganizationName = (x.OrganizationId.Value != null) ? _context.Organizations
                .Where(o => o.Id == x.OrganizationId.Value)
                .Select(o => o.Name)
                .FirstOrDefault() : "INTERNAL",
                    Role = x.Role.ToString(),
                    Status = x.Status.ToString(),
                    CreatedAt = x.CreatedAt
                })
                .ToListAsync(ct);
            var result = data.Select((x, index) =>
            {
                x.Stt = (filter.PageIndex - 1) * filter.PageSize + index + 1;
                return x;
            }).ToList();

            return new PagedResult<AdminAccountListDto>
            {
                PageIndex = filter.PageIndex,
                PageSize = filter.PageSize,
                Total = total,
                Items = result
            };
        }

    }
}
