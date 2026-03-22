using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Universities.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Universities.Queries.GetUniversityById;

public class GetUniversityByIdHandler : IRequestHandler<GetUniversityByIdQuery, Result<GetUniversityByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUniversityByIdHandler> _logger;
    private readonly ICacheService _cacheService;

    public GetUniversityByIdHandler(IUnitOfWork unitOfWork, ILogger<GetUniversityByIdHandler> logger, ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _cacheService = cacheService;
    }

    public async Task<Result<GetUniversityByIdResponse>> Handle(GetUniversityByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start GetUniversityByIdQuery for ID: {UniversityId}", request.UniversityId);

        var cacheKey = UniversityCacheKeys.University(request.UniversityId);
        var cached = await _cacheService.GetAsync<GetUniversityByIdResponse>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result<GetUniversityByIdResponse>.Success(cached);

        var university = await _unitOfWork.Repository<University>().GetByIdAsync(request.UniversityId);

        if (university == null || university.DeletedAt != null)
        {
            _logger.LogWarning("University with ID {UniversityId} not found or deleted", request.UniversityId);
            throw new NotFoundException(nameof(University), request.UniversityId);
        }

        var response = new GetUniversityByIdResponse
        {
            UniversityId = university.UniversityId,
            Code = university.Code,
            Name = university.Name,
            Address = university.Address,
            LogoUrl = university.LogoUrl,
            Status = university.Status
        };

        _logger.LogInformation("Successfully retrieved university: {Name}", university.Name);

        await _cacheService.SetAsync(cacheKey, response, UniversityCacheKeys.Expiration.University, cancellationToken);

        return Result<GetUniversityByIdResponse>.Success(response);
    }
}
