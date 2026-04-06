using IOCv2.Application.Constants;
using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Features.Universities.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Universities.Commands.DeleteUniversity;

public class DeleteUniversityHandler : IRequestHandler<DeleteUniversityCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteUniversityHandler> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICacheService _cacheService;
    private readonly IMessageService _messageService;

    public DeleteUniversityHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteUniversityHandler> logger,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        IMessageService messageService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _messageService = messageService;
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

        // BR-UNI-DL-02: Dependency Guard
        // 1) Refuse delete if university has active terms (Open)
        var hasActiveTerms = await _unitOfWork.Repository<Term>()
            .Query()
            .AnyAsync(t => t.UniversityId == request.UniversityId && t.Status == TermStatus.Open, cancellationToken);

        if (hasActiveTerms)
        {
            return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.University.HasActiveTerms), ResultErrorType.Forbidden);
        }

        // 2) Refuse delete if any student is currently interning under those terms
        var hasInterningStudents = await _unitOfWork.Repository<StudentTerm>()
            .Query()
            .AnyAsync(
                st => st.Term.UniversityId == request.UniversityId &&
                      st.Student.InternshipStatus == StudentStatus.INTERNSHIP_IN_PROGRESS,
                cancellationToken);

        if (hasInterningStudents)
        {
            return Result<bool>.Failure(_messageService.GetMessage(MessageKeys.University.HasInterningStudents), ResultErrorType.Forbidden);
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            university.Delete();
            university.UpdatedBy = _currentUserService.UserId != null ? Guid.Parse(_currentUserService.UserId) : null;

            await _unitOfWork.Repository<University>().UpdateAsync(university);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();

            await _cacheService.RemoveByPatternAsync(UniversityCacheKeys.UniversityListPattern(), cancellationToken);
            await _cacheService.RemoveAsync(UniversityCacheKeys.University(request.UniversityId), cancellationToken);

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
