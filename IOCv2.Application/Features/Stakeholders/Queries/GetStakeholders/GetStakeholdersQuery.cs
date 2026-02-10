using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Extensions.Query;
using IOCv2.Application.Features.Stakeholders.DTOs;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Localization;

namespace IOCv2.Application.Features.Stakeholders.Queries.GetStakeholders;

public record GetStakeholdersQuery(Guid ProjectId, PaginationParams Pagination) 
    : IRequest<Result<PagedResult<StakeholderDto>>>;

public class GetStakeholdersQueryHandler 
    : IRequestHandler<GetStakeholdersQuery, Result<PagedResult<StakeholderDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IStringLocalizer<Resources.ErrorMessages> _localizer;

    public GetStakeholdersQueryHandler(
        IUnitOfWork unitOfWork, 
        IMapper mapper,
        IStringLocalizer<Resources.ErrorMessages> localizer)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _localizer = localizer;
    }

    public async Task<Result<PagedResult<StakeholderDto>>> Handle(
        GetStakeholdersQuery request, 
        CancellationToken cancellationToken)
    {
        // Check if project exists
        var projectExists = await _unitOfWork.Repository<Project>()
            .ExistsAsync(p => p.Id == request.ProjectId && p.DeletedAt == null, cancellationToken);

        if (!projectExists)
        {
            return Result<PagedResult<StakeholderDto>>.NotFound(_localizer["Stakeholder.ProjectNotFound"]);
        }

        // Build query
        var query = _unitOfWork.Repository<Stakeholder>()
            .Query()
            .Where(s => s.ProjectId == request.ProjectId && s.DeletedAt == null);

        // Apply search
        if (!string.IsNullOrWhiteSpace(request.Pagination.Search))
        {
            var searchLower = request.Pagination.Search.ToLower();
            query = query.Where(s => 
                s.Name.ToLower().Contains(searchLower) ||
                (s.Role != null && s.Role.ToLower().Contains(searchLower)) ||
                s.Email.ToLower().Contains(searchLower));
        }

        // Apply sorting
        query = !string.IsNullOrWhiteSpace(request.Pagination.OrderBy) 
            ? request.Pagination.OrderBy.ToLower() switch
            {
                "name" => query.OrderBy(s => s.Name),
                "name_desc" => query.OrderByDescending(s => s.Name),
                "email" => query.OrderBy(s => s.Email),
                "email_desc" => query.OrderByDescending(s => s.Email),
                "createdat" => query.OrderBy(s => s.CreatedAt),
                "createdat_desc" => query.OrderByDescending(s => s.CreatedAt),
                _ => query.OrderBy(s => s.CreatedAt)
            }
            : query.OrderBy(s => s.CreatedAt);

        // Project and paginate
        var result = await query
            .ProjectTo<StakeholderDto>(_mapper.ConfigurationProvider)
            .ToPagedResultAsync(request.Pagination);

        return Result<PagedResult<StakeholderDto>>.Success(result);
    }
}

