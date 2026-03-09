using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Terms.Commands.CloseTerm;

public class CloseTermHandler : IRequestHandler<CloseTermCommand, Result<CloseTermResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CloseTermHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;

    public CloseTermHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<CloseTermHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<CloseTermResponse>> Handle(CloseTermCommand request, CancellationToken cancellationToken)
    {
        try
        {
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
                    return Result<CloseTermResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.University.NotFound),
                        ResultErrorType.NotFound);

                term = await _unitOfWork.Repository<Term>()
                    .Query()
                    .Where(t => t.TermId == request.TermId && t.UniversityId == universityUser.UniversityId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (term == null)
                return Result<CloseTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.NotFound),
                    ResultErrorType.NotFound);

            // Check version
            if (term.Version != request.Version)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Terms.LogVersionConflict), request.TermId);
                return Result<CloseTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.VersionConflict),
                    ResultErrorType.Conflict);
            }

            // Check if term is Active using TermStatusHelper
            if (!TermStatusHelper.IsActive(term.StartDate, term.EndDate, term.Status))
                return Result<CloseTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.OnlyActiveCanBeClosed));

            // Close term
            term.Status = TermStatus.Closed;
            term.ClosedBy = userId;
            term.ClosedAt = DateTime.UtcNow;
            term.Version++;

            await _unitOfWork.Repository<Term>().UpdateAsync(term, cancellationToken);
            await _unitOfWork.SaveChangeAsync(cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Terms.LogTermClosed), term.TermId, userId);

            var message = string.Format(_messageService.GetMessage(MessageKeys.Terms.CloseSuccess), term.Name);

            var response = new CloseTermResponse
            {
                Message = message,
                UnplacedStudentsCount = term.TotalUnplaced
            };

            // Check for unplaced students warning
            if (term.TotalUnplaced > 0)
                response.Warning = string.Format(_messageService.GetMessage(MessageKeys.Terms.UnplacedStudentsWarning),
                    term.TotalUnplaced);

            return Result<CloseTermResponse>.Success(response, message);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogWarning(ex, _messageService.GetMessage(MessageKeys.Terms.LogConcurrencyConflictClosing),
                request.TermId);
            return Result<CloseTermResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Terms.VersionConflict),
                ResultErrorType.Conflict);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Terms.LogErrorClosingTerm), request.TermId);
            throw;
        }
    }
}