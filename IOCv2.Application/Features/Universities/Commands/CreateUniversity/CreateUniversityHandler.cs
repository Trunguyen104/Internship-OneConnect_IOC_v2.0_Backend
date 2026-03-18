using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Universities.Commands.CreateUniversity;

public class CreateUniversityHandler : IRequestHandler<CreateUniversityCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateUniversityHandler> _logger;
    private readonly ICurrentUserService _currentUserService;

    public CreateUniversityHandler(
        IUnitOfWork unitOfWork, 
        ILogger<CreateUniversityHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(CreateUniversityCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start CreateUniversityCommand: {Name} ({Code})", request.Name, request.Code);

        // Transaction phase (as per FFA-TXG)
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var university = University.Create(
                request.Code,
                request.Name,
                request.Address,
                null);


            await _unitOfWork.Repository<University>().AddAsync(university);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            
            await _unitOfWork.CommitTransactionAsync();

            _logger.LogInformation("Successfully created university: {UniversityId}", university.UniversityId);

            return Result<Guid>.Success(university.UniversityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating university: {Message}", ex.Message);
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
