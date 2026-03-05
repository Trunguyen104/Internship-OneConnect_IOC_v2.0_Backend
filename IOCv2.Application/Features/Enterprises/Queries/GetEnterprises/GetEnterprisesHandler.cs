using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Enterprises.Queries.GetEnterpriseById;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.RateLimiting;
using System.Threading.Tasks;

namespace IOCv2.Application.Features.Enterprises.Queries.GetEnterprises
{
    public class GetEnterprisesHandler : MediatR.IRequestHandler<GetEnterprisesQuery, Result<PaginatedResult<GetEnterprisesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetEnterprisesHandler> _logger;
        private readonly IMapper _mapper;
        private readonly IRateLimiter _rateLimiter;
        private readonly ICurrentUserService _currentUserService;
        public GetEnterprisesHandler(IUnitOfWork unitOfWork, IMessageService messageService, ILogger<GetEnterprisesHandler> logger, IMapper mapper, ICurrentUserService currentUserService, IRateLimiter rateLimiter) {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _logger = logger;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _rateLimiter = rateLimiter;
        }
        public async Task<Result<PaginatedResult<GetEnterprisesResponse>>> Handle(GetEnterprisesQuery request, CancellationToken cancellationToken)
        {
            try
            {
                // Each user has own key counting invalid turn
                var rateLimitKey = $"get_enterprise_attempt:{_currentUserService.UserId}";
                // Check if user is blocked due to too many failed attempts
                if (await _rateLimiter.IsBlockedAsync(rateLimitKey, cancellationToken))
                {
                    return Result<PaginatedResult<GetEnterprisesResponse>>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.RequestManyTimes));
                }
                // Register failed attempt (block after 30 attempts in 1 mins)
                await _rateLimiter.RegisterFailAsync(
                    rateLimitKey,
                    limit: 30,
                    window: TimeSpan.FromMinutes(1),
                    blockFor: TimeSpan.FromMinutes(1),
                    cancellationToken);
                // Log the incoming request parameters
                var query = _unitOfWork.Repository<Enterprise>()
                    .Query()
                    .AsNoTracking()
                    .AsQueryable();
                // Apply filters based on the request parameters
                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var keyword = request.SearchTerm.Trim().ToLower();

                    query = query.Where(e =>
                        e.Name.ToLower().Contains(keyword) ||
                        (e.TaxCode != null && e.TaxCode.ToLower().Contains(keyword)) ||
                        (e.Industry != null && e.Industry.ToLower().Contains(keyword)) ||
                        (e.Description != null && e.Description.ToLower().Contains(keyword)) ||
                        (e.Address != null && e.Address.ToLower().Contains(keyword)) ||
                        (e.Website != null && e.Website.ToLower().Contains(keyword)));
                }

                if (!string.IsNullOrWhiteSpace(request.TaxCode)) query = query.Where(e => e.TaxCode == request.TaxCode);
                if (!string.IsNullOrWhiteSpace(request.Name)) query = query.Where(e => e.Name.Contains(request.Name));
                if (!string.IsNullOrWhiteSpace(request.Industry)) query = query.Where(e => e.Industry == request.Industry);
                if (request.IsVerified.HasValue) query = query.Where(e => e.IsVerified == request.IsVerified.Value);
                if (request.Status.HasValue) query = query.Where(e => e.Status == (short) request.Status.Value);
                query = ApplySorting(query, request.SortColumn, request.SortOrder);
                // Get total count before pagination
                var totalCount = await query.CountAsync(cancellationToken);
                // Apply pagination
                var items = await query.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
                    .ProjectTo<GetEnterprisesResponse>(_mapper.ConfigurationProvider).ToListAsync(cancellationToken);
                var result = PaginatedResult<GetEnterprisesResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);
                return Result<PaginatedResult<GetEnterprisesResponse>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Enterprise.GetEnterprisesError));
                return Result<PaginatedResult<GetEnterprisesResponse>>.Failure(_messageService.GetMessage(MessageKeys.Enterprise.GetEnterprisesError), ResultErrorType.InternalServerError);
            }
        }
        private static IQueryable<Enterprise> ApplySorting(
            IQueryable<Enterprise> query,
            string? sortColumn,
            string? sortOrder)
        {
            if (string.IsNullOrWhiteSpace(sortColumn))
                return query.OrderByDescending(e => e.Name);

            var isDesc = sortOrder?.ToLower() == "desc";

            return sortColumn.ToLower() switch
            {
                "name" => isDesc
                    ? query.OrderByDescending(e => e.Name)
                    : query.OrderBy(e => e.Name),

                "taxcode" => isDesc
                    ? query.OrderByDescending(e => e.TaxCode)
                    : query.OrderBy(e => e.TaxCode),

                "industry" => isDesc
                    ? query.OrderByDescending(e => e.Industry)
                    : query.OrderBy(e => e.Industry),

                "status" => isDesc
                    ? query.OrderByDescending(e => e.Status)
                    : query.OrderBy(e => e.Status),

                "isverified" => isDesc
                    ? query.OrderByDescending(e => e.IsVerified)
                    : query.OrderBy(e => e.IsVerified),

                _ => query.OrderByDescending(e => e.Name)
            };
        }
    }
}
