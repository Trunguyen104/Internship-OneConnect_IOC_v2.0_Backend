using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Extensions.Pagination;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Epics.Queries.GetEpics;

public class GetEpicsHandler : IRequestHandler<GetEpicsQuery, Result<PagedResult<GetEpicsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    
    public GetEpicsHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }
    
    public async Task<Result<PagedResult<GetEpicsResponse>>> Handle(GetEpicsQuery request, CancellationToken cancellationToken)
    {
        // Query Epics for specific project
        var query = _unitOfWork.Repository<WorkItem>()
            .FindAsync(w => w.ProjectId == request.ProjectId && w.Type == WorkItemType.Epic, cancellationToken)
            .Result
            .AsQueryable();
        
        // Apply search if provided
        if (!string.IsNullOrWhiteSpace(request.Pagination.Search))
        {
            var searchTerm = request.Pagination.Search.ToLower();
            query = query.Where(w => 
                w.Title.ToLower().Contains(searchTerm) ||
                (w.Description != null && w.Description.ToLower().Contains(searchTerm))
            );
        }
        
        // Get total count
        var totalCount = query.Count();
        
        // Apply ordering (default: by CreatedAt descending)
        query = string.IsNullOrWhiteSpace(request.Pagination.OrderBy)
            ? query.OrderByDescending(w => w.CreatedAt)
            : request.Pagination.OrderBy.ToLower() switch
            {
                "title" => query.OrderBy(w => w.Title),
                "title_desc" => query.OrderByDescending(w => w.Title),
                "createdat" => query.OrderBy(w => w.CreatedAt),
                "createdat_desc" => query.OrderByDescending(w => w.CreatedAt),
                _ => query.OrderByDescending(w => w.CreatedAt)
            };
        
        // Apply pagination
        var items = query
            .Skip((request.Pagination.PageIndex - 1) * request.Pagination.PageSize)
            .Take(request.Pagination.PageSize)
            .ToList();
        
        // Map to response DTO
        var mappedItems = _mapper.Map<List<GetEpicsResponse>>(items);
        
        var pagedResult = new PagedResult<GetEpicsResponse>(
            mappedItems,
            request.Pagination,
            totalCount
        );
        
        return Result<PagedResult<GetEpicsResponse>>.Success(pagedResult);
    }
}
