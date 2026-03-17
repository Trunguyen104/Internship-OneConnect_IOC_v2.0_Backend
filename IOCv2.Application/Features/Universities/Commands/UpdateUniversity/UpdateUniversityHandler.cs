using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
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

    public UpdateUniversityHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateUniversityHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
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
            university.UpdateInfo(
                request.Code,
                request.Name,
                request.Address,
                request.LogoUrl);

            university.UpdatedBy = _currentUserService.UserId != null ? Guid.Parse(_currentUserService.UserId) : null;

            await _unitOfWork.Repository<University>().UpdateAsync(university);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();

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
