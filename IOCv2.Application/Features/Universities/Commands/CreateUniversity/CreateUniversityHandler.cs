using IOCv2.Application.Common.Models;
using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Features.Universities.Common;
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
    private readonly ICacheService _cacheService;

    private readonly IEmailService _emailService;
    private readonly IBackgroundEmailSender _emailSender;

    public CreateUniversityHandler(
        IUnitOfWork unitOfWork,
        ILogger<CreateUniversityHandler> logger,
        ICurrentUserService currentUserService,
        ICacheService cacheService,
        IEmailService emailService,
        IBackgroundEmailSender emailSender)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
        _emailService = emailService;
        _emailSender = emailSender;
    }

    public async Task<Result<Guid>> Handle(CreateUniversityCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Start CreateUniversityCommand: {Name} ({Code})", request.Name, request.Code);

        // Transaction phase (as per FFA-TXG)
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            // TX-6: Pre-check uniqueness before write
            var exists = await _unitOfWork.Repository<University>()
                .ExistsAsync(u => u.Code == request.Code, cancellationToken);
            
            if (exists)
            {
                throw new ConflictException("University code already exists", "Code");
            }

            var university = University.Create(
                request.Code.Trim(),
                request.Name.Trim(),
                request.Address?.Trim(),
                null,
                request.ContactEmail?.Trim());

            await _unitOfWork.Repository<University>().AddAsync(university, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            await _unitOfWork.CommitTransactionAsync();

            await _cacheService.RemoveByPatternAsync(UniversityCacheKeys.UniversityListPattern(), cancellationToken);

            _logger.LogInformation("Successfully created university: {UniversityId}", university.UniversityId);

            // Send notification email asynchronously via background channel
            if (!string.IsNullOrWhiteSpace(university.ContactEmail))
            {
                await _emailSender.EnqueueUniversityCreationEmailAsync(
                    university.ContactEmail,
                    university.Name,
                    university.Code,
                    university.UniversityId,
                    null,
                    cancellationToken);
            }

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
