using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Projects.Queries.GetProjects;

public class GetProjectsHandler : IRequestHandler<GetProjectsQuery, Result<PaginatedResult<GetProjectsResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetProjectsHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedResult<GetProjectsResponse>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        var query = _unitOfWork.Repository<Project>().Query()
            .AsNoTracking();

        // 1. Search term
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.ProjectName.ToLower().Contains(term) ||
                (p.Tags != null && p.Tags.ToLower().Contains(term)) ||
                p.Field.ToLower().Contains(term)
            );
        }

        // 2. Filter Status
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<ProjectStatus>(request.Status, true, out var status))
            {
                query = query.Where(p => p.Status == status);
            }
        }

        // 3. Sorting
        query = (request.SortColumn?.ToLower(), request.SortOrder?.ToLower()) switch
        {
            ("name", "desc") => query.OrderByDescending(p => p.ProjectName),
            ("name", _) => query.OrderBy(p => p.ProjectName),
            ("viewcount", "desc") => query.OrderByDescending(p => p.ViewCount),
            ("viewcount", _) => query.OrderBy(p => p.ViewCount),
            ("startdate", "desc") => query.OrderByDescending(p => p.StartDate),
            ("startdate", _) => query.OrderBy(p => p.StartDate),
            _ => query.OrderByDescending(p => p.CreatedAt)
        };

        // 4. Count for pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // 5. Projection
        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ProjectTo<GetProjectsResponse>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        var result = PaginatedResult<GetProjectsResponse>.Create(
            items, totalCount, request.PageNumber, request.PageSize);

        return Result<PaginatedResult<GetProjectsResponse>>.Success(result);
    }
}
