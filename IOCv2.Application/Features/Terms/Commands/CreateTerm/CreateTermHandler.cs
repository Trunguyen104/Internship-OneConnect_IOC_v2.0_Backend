using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Terms.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Terms.Commands.CreateTerm;

public class CreateTermHandler : IRequestHandler<CreateTermCommand, Result<CreateTermResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateTermHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public CreateTermHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ILogger<CreateTermHandler> logger,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<Result<CreateTermResponse>> Handle(CreateTermCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin =
                string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            Guid universityId;

            if (isSuperAdmin)
            {
                // SuperAdmin must specify the target university
                if (!request.UniversityId.HasValue)
                {
                    return Result<CreateTermResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Terms.UniversityIdRequired),
                        ResultErrorType.BadRequest);
                }

                var universityExists = await _unitOfWork.Repository<University>()
                    .ExistsAsync(u => u.UniversityId == request.UniversityId.Value, cancellationToken);
                if (!universityExists)
                    return Result<CreateTermResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.University.NotFound),
                        ResultErrorType.NotFound);

                universityId = request.UniversityId.Value;
            }
            else
            {
                // SchoolAdmin: resolve university from UniversityUser table
                var universityUser = await _unitOfWork.Repository<UniversityUser>()
                    .Query()
                    .Where(uu => uu.UserId == userId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (universityUser == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.Terms.LogUserNotAssociatedWithUniversity),
                        userId);
                    return Result<CreateTermResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.University.NotFound),
                        ResultErrorType.NotFound);
                }

                universityId = universityUser.UniversityId;
            }

            // Check for overlapping terms (single query returns name if conflict exists)
            var overlappingTermName = await _unitOfWork.Repository<Term>()
                .Query()
                .Where(t => t.UniversityId == universityId)
                .Where(t => t.Status == TermStatus.Open || t.Status == TermStatus.Closed)
                .Where(t =>
                    (request.StartDate >= t.StartDate && request.StartDate <= t.EndDate) ||
                    (request.EndDate >= t.StartDate && request.EndDate <= t.EndDate) ||
                    (request.StartDate <= t.StartDate && request.EndDate >= t.EndDate))
                .Select(t => (string?)t.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (overlappingTermName != null)
            {
                return Result<CreateTermResponse>.Failure(
                    string.Format(_messageService.GetMessage(MessageKeys.Terms.OverlapWithActiveTerm), overlappingTermName),
                    ResultErrorType.Conflict);
            }

            // Determine initial status
            var initialStatus = TermStatus.Open;

            // Create term
            var term = new Term
            {
                TermId = Guid.NewGuid(),
                UniversityId = universityId,
                Name = request.Name.Trim(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                Status = initialStatus,
                Version = 1,
                TotalEnrolled = 0,
                TotalPlaced = 0,
                TotalUnplaced = 0
            };

            await _unitOfWork.Repository<Term>().AddAsync(term, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync(TermCacheKeys.TermListPattern(), cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Terms.LogTermCreated), term.TermId, userId);

            var response = _mapper.Map<CreateTermResponse>(term);
            return Result<CreateTermResponse>.Success(response,
                _messageService.GetMessage(MessageKeys.Terms.CreateSuccess));
        }
        
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Terms.LogErrorCreatingTerm));
            throw;
        }
    }
}