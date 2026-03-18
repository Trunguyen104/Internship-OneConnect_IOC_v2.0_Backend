using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Universities.Queries.GetUniversityById;

public class GetUniversityByIdHandler : IRequestHandler<GetUniversityByIdQuery, Result<GetUniversityByIdResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GetUniversityByIdHandler> _logger;

    public GetUniversityByIdHandler(IUnitOfWork unitOfWork, ILogger<GetUniversityByIdHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<GetUniversityByIdResponse>> Handle(GetUniversityByIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start GetUniversityByIdQuery for ID: {UniversityId}", request.UniversityId);

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

        return Result<GetUniversityByIdResponse>.Success(response);
    }
}
