using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Universities.Commands.DeleteUniversity;

public class DeleteUniversityHandler : IRequestHandler<DeleteUniversityCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteUniversityHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public DeleteUniversityHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteUniversityHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<bool>> Handle(DeleteUniversityCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start DeleteUniversityCommand for ID: {UniversityId}", request.UniversityId);

        var university = await _unitOfWork.Repository<University>().GetByIdAsync(request.UniversityId);

        if (university == null || university.DeletedAt != null)
        {
            _logger.LogWarning("University with ID {UniversityId} not found for deletion", request.UniversityId);
            throw new NotFoundException(nameof(University), request.UniversityId);
        }

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            university.Delete();
            university.UpdatedBy = _currentUserService.UserId != null ? Guid.Parse(_currentUserService.UserId) : null;

            await _unitOfWork.Repository<University>().UpdateAsync(university);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Successfully deleted university: {UniversityId}", university.UniversityId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting university {UniversityId}: {Message}", request.UniversityId, ex.Message);
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
