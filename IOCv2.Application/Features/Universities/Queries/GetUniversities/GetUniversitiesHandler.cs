using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Universities.Queries.GetUniversities;

public class GetUniversitiesHandler : IRequestHandler<GetUniversitiesQuery, Result<PaginatedResult<GetUniversitiesResponse>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUniversitiesHandler> _logger;

    public GetUniversitiesHandler(IUnitOfWork unitOfWork, ILogger<GetUniversitiesHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<PaginatedResult<GetUniversitiesResponse>>> Handle(GetUniversitiesQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start GetUniversitiesQuery with PageNumber: {PageNumber}, PageSize: {PageSize}", request.PageNumber, request.PageSize);

        var query = _unitOfWork.Repository<University>().Query()
            .Where(u => u.DeletedAt == null)
            .AsNoTracking();

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(u => u.Name.ToLower().Contains(search) || u.Code.ToLower().Contains(search));
        }

        if (request.Status.HasValue)
        {
            query = query.Where(u => u.Status == request.Status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        
        var items = await query
            .OrderBy(u => u.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new GetUniversitiesResponse
            {
                UniversityId = u.UniversityId,
                Code = u.Code,
                Name = u.Name,
                Address = u.Address,
                LogoUrl = u.LogoUrl,
                Status = u.Status
            })
            .ToListAsync(cancellationToken);

        var result = PaginatedResult<GetUniversitiesResponse>.Create(items, totalCount, request.PageNumber, request.PageSize);

        _logger.LogInformation("Successfully retrieved {Count} universities", items.Count);

        return Result<PaginatedResult<GetUniversitiesResponse>>.Success(result);
    }
}
