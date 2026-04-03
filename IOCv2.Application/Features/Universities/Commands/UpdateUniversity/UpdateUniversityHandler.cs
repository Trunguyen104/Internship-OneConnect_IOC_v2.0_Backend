using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Universities.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Universities.Commands.UpdateUniversity;

public class UpdateUniversityHandler : IRequestHandler<UpdateUniversityCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateUniversityHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;

    public UpdateUniversityHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateUniversityHandler> logger,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<Result<bool>> Handle(UpdateUniversityCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start UpdateUniversityCommand for ID: {UniversityId}", request.UniversityId);

        var university = await _unitOfWork.Repository<University>().GetByIdAsync(request.UniversityId);

        if (university == null || university.DeletedAt != null)
        {
            _logger.LogWarning("University with ID {UniversityId} not found for update", request.UniversityId);
            throw new NotFoundException(nameof(University), request.UniversityId);
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // TX-6: Pre-check uniqueness before write
            var exists = await _unitOfWork.Repository<University>()
                .ExistsAsync(u => u.Code == request.Code && u.UniversityId != request.UniversityId, cancellationToken);
            
            if (exists)
            {
                throw new ConflictException("University code already exists", "Code");
            }

            university.UpdateInfo(
                request.Code,
                request.Name,
                request.Address,
                request.LogoUrl,
                request.ContactEmail);

            university.UpdatedBy = _currentUserService.UserId != null ? Guid.Parse(_currentUserService.UserId) : null;

            await _unitOfWork.Repository<University>().UpdateAsync(university);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();

            await _cacheService.RemoveByPatternAsync(UniversityCacheKeys.UniversityListPattern(), cancellationToken);
            await _cacheService.RemoveAsync(UniversityCacheKeys.University(request.UniversityId), cancellationToken);

            _logger.LogInformation("Successfully updated university: {UniversityId}", university.UniversityId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating university {UniversityId}: {Message}", request.UniversityId, ex.Message);
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
