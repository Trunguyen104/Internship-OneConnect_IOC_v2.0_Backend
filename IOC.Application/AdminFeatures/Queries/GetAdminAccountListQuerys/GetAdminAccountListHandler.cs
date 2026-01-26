using IOC.Application.AdminFeatures.DTOs;
using IOC.Application.Commons.Interfaces.Repositories;
using IOC.Application.Commons.Interfaces.Services;
using IOC.Application.Commons.Models.Paging;
using IOC.Domain.Enums;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IOC.Application.AdminFeatures.Queries.GetAdminAccountListQuerys
{
    public class GetAdminAccountListHandler
    : IRequestHandler<GetAdminAccountListQuery, PagedResult<AdminAccountListDto>>
    {
        private readonly IAdminAccountRepository _repo;
        private readonly ICurrentUserService _currentUser;

        public GetAdminAccountListHandler(
            IAdminAccountRepository repo,
            ICurrentUserService currentUser)
        {
            _repo = repo;
            _currentUser = currentUser;
        }

        public Task<PagedResult<AdminAccountListDto>> Handle(
            GetAdminAccountListQuery request,
            CancellationToken ct)
        {
            // Build filter from request
            var filter = new AdminAccountFilter
            {
                Keyword = request.Keyword,
                Email = request.Email,
                Code = request.Code,
                OrganizationId = request.OrganizationId,
                Role = request.Role,
                Status = request.Status,
                CreatedFrom = request.CreatedFrom,
                CreatedTo = request.CreatedTo,
                SortBy = request.SortBy,
                SortDir = request.SortDir,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };

            var role = _currentUser.Role;
            if (role == null)
                throw new UnauthorizedAccessException("Unauthenticated");

            return _repo.GetListAsync(filter, ct);
        }
    }

}