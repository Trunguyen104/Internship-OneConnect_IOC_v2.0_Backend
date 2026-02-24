using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Stakeholders.DTOs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders
{
    public class GetStakeholdersHandler : IRequestHandler<GetStakeholdersQuery, Result<PaginatedResult<StakeholderDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public GetStakeholdersHandler(IUnitOfWork unitOfWork, IMapper mapper, IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<PaginatedResult<StakeholderDto>>> Handle(GetStakeholdersQuery request, CancellationToken cancellationToken)
        {
            // Check project exists
            var projectExists = await _unitOfWork.Repository<Project>()
                .ExistsAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (!projectExists)
                return Result<PaginatedResult<StakeholderDto>>.NotFound(
                    _messageService.GetMessage(MessageKeys.Stakeholder.ProjectNotFound));

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
                .ProjectTo<StakeholderDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            var result = PaginatedResult<StakeholderDto>.Create(items, totalCount, request.PageNumber, request.PageSize);
            return Result<PaginatedResult<StakeholderDto>>.Success(result);
        }
    }
}

