﻿using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders
{
    public class GetStakeholdersHandler : IRequestHandler<GetStakeholdersQuery, Result<PaginatedResult<GetStakeholdersResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetStakeholdersHandler> _logger;

        public GetStakeholdersHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMessageService messageService,
            ILogger<GetStakeholdersHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<PaginatedResult<GetStakeholdersResponse>>> Handle(GetStakeholdersQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting paginated stakeholders for project {ProjectId}", request.ProjectId);

            // Check project exists and user has access
            var projectExists = await _unitOfWork.Repository<Project>()
                .ExistsAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (!projectExists)
            {
                _logger.LogWarning("Project {ProjectId} not found", request.ProjectId);
                return Result<PaginatedResult<GetStakeholdersResponse>>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.ProjectNotFound));
            }

            // TODO: Ownership check

            // Build base query
            var query = _unitOfWork.Repository<Stakeholder>()
                .Query()
                .Where(s => s.ProjectId == request.ProjectId)
                .AsNoTracking();

            // Filter by search term
            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var term = request.SearchTerm.Trim().ToLower();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    (s.Role != null && s.Role.ToLower().Contains(term)) ||
                    s.Email.ToLower().Contains(term));
            }

            // Sorting
            query = (request.SortColumn?.ToLower(), request.SortOrder?.ToLower()) switch
            {
                ("name", "desc")        => query.OrderByDescending(s => s.Name),
                ("name", _)             => query.OrderBy(s => s.Name),
                ("email", "desc")       => query.OrderByDescending(s => s.Email),
                ("email", _)            => query.OrderBy(s => s.Email),
                ("createdat", "desc")   => query.OrderByDescending(s => s.CreatedAt),
                ("createdat", _)        => query.OrderBy(s => s.CreatedAt),
                _                       => query.OrderBy(s => s.CreatedAt)
            };

            var totalCount = await query.CountAsync(cancellationToken);

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ProjectTo<GetStakeholdersResponse>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            _logger.LogInformation("Successfully retrieved {Count} stakeholders for project {ProjectId}", items.Count, request.ProjectId);

            var result = PaginatedResult<GetStakeholdersResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<GetStakeholdersResponse>>.Success(result);
        }
    }
}

