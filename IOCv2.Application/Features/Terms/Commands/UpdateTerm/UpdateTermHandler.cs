using AutoMapper;
using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Enterprises.Common;
using IOCv2.Application.Features.Terms.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Terms.Commands.UpdateTerm;

public class UpdateTermHandler : IRequestHandler<UpdateTermCommand, Result<UpdateTermResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UpdateTermHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public UpdateTermHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ILogger<UpdateTermHandler> logger,
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

    public async Task<Result<UpdateTermResponse>> Handle(UpdateTermCommand request, CancellationToken cancellationToken)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin =
                string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            Term? term;

            if (isSuperAdmin)
            {
                // SuperAdmin: access any term regardless of university
                term = await _unitOfWork.Repository<Term>()
                    .Query()
                    .Where(t => t.TermId == request.TermId)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                // SchoolAdmin: resolve university then restrict to their own
                var universityUser = await _unitOfWork.Repository<UniversityUser>()
                    .Query()
                    .Where(uu => uu.UserId == userId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (universityUser == null)
                    return Result<UpdateTermResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.University.NotFound),
                        ResultErrorType.NotFound);

                term = await _unitOfWork.Repository<Term>()
                    .Query()
                    .Where(t => t.TermId == request.TermId && t.UniversityId == universityUser.UniversityId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            // Get existing term
            if (term == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Terms.LogTermNotFound), request.TermId);
                return Result<UpdateTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.NotFound),
                    ResultErrorType.NotFound);
            }

            // Capture universityId from loaded term for overlap checks
            var universityId = term.UniversityId;

            // Check version for optimistic locking
            if (term.Version != request.Version)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Terms.LogVersionConflictDetailed),
                    request.TermId, term.Version, request.Version);
                return Result<UpdateTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.VersionConflict),
                    ResultErrorType.Conflict);
            }

            // Check if StartDate is locked (cannot change if ACTIVE or ENDED) using TermStatusHelper
            var isActiveOrEnded = TermStatusHelper.IsActive(term.StartDate, term.EndDate, term.Status) ||
                                  TermStatusHelper.IsEnded(term.StartDate, term.EndDate, term.Status);

            if (isActiveOrEnded && term.StartDate != request.StartDate)
                return Result<UpdateTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.StartDateLocked));

            // Check for overlapping terms (exclude current term, single query)
            var overlappingTermName = await _unitOfWork.Repository<Term>()
                .Query()
                .Where(t => t.UniversityId == universityId && t.TermId != request.TermId)
                .Where(t => t.Status == TermStatus.Open || t.Status == TermStatus.Closed)
                .Where(t =>
                    (request.StartDate >= t.StartDate && request.StartDate <= t.EndDate) ||
                    (request.EndDate >= t.StartDate && request.EndDate <= t.EndDate) ||
                    (request.StartDate <= t.StartDate && request.EndDate >= t.EndDate))
                .Select(t => (string?)t.Name)
                .FirstOrDefaultAsync(cancellationToken);

            if (overlappingTermName != null)
            {
                return Result<UpdateTermResponse>.Failure(
                    string.Format(_messageService.GetMessage(MessageKeys.Terms.OverlapWithActiveTerm), overlappingTermName),
                    ResultErrorType.Conflict);
            }

            // Update term
            term.Name = request.Name.Trim();
            term.StartDate = request.StartDate;
            term.EndDate = request.EndDate;
            term.Version++; // Increment version

            await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync(TermCacheKeys.TermListPattern(), cancellationToken);
            await _cacheService.RemoveByPatternAsync(TermCacheKeys.TermDetailPattern(), cancellationToken);
            // EndDate thay đổi có thể làm term không còn Active → invalidate enterprise cache
            await _cacheService.RemoveByPatternAsync(EnterpriseCacheKeys.AllActivePhasesPattern(), cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Terms.LogTermUpdated), term.TermId, userId);

            var response = _mapper.Map<UpdateTermResponse>(term);
            return Result<UpdateTermResponse>.Success(response,
                _messageService.GetMessage(MessageKeys.Terms.UpdateSuccess));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Terms.LogErrorUpdatingTerm), request.TermId);
            return Result<UpdateTermResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError),
                ResultErrorType.InternalServerError);
        }
    }
}