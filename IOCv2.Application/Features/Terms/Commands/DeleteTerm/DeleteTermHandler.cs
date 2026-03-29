using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Terms.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Terms.Commands.DeleteTerm;

public class DeleteTermHandler : IRequestHandler<DeleteTermCommand, Result<DeleteTermResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DeleteTermHandler> _logger;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public DeleteTermHandler(
        IUnitOfWork unitOfWork,
        IMessageService messageService,
        ILogger<DeleteTermHandler> logger,
        ICurrentUserService currentUserService,
        ICacheService cacheService)
    {
        _unitOfWork = unitOfWork;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
        _cacheService = cacheService;
    }

    public async Task<Result<DeleteTermResponse>> Handle(DeleteTermCommand request, CancellationToken cancellationToken)
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
                    .IgnoreQueryFilters()
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
                    return Result<DeleteTermResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.University.NotFound),
                        ResultErrorType.NotFound);

                term = await _unitOfWork.Repository<Term>()
                    .Query()
                    .IgnoreQueryFilters()
                    .Where(t => t.TermId == request.TermId && t.UniversityId == universityUser.UniversityId)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (term == null)
                return Result<DeleteTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.NotFound),
                    ResultErrorType.NotFound);

            // Check if already deleted
            if (term.DeletedAt.HasValue)
                return Result<DeleteTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.AlreadyDeleted),
                    ResultErrorType.Conflict);

            // Check computed status - only Upcoming can be deleted using TermStatusHelper
            if (!TermStatusHelper.IsUpcoming(term.StartDate, term.EndDate, term.Status))
                return Result<DeleteTermResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.OnlyUpcomingCanBeDeleted));

            // Begin transaction for cascade delete
            await _unitOfWork.BeginTransactionAsync(cancellationToken);

            // Check for related data
            var studentTermsCount = await _unitOfWork.Repository<StudentTerm>()
                .Query()
                .IgnoreQueryFilters()
                .CountAsync(st => st.TermId == request.TermId, cancellationToken);

            // Soft delete term
            await _unitOfWork.Repository<Term>().DeleteAsync(term, cancellationToken);

            // If has related data, also soft delete them
            if (studentTermsCount > 0)
            {
                var studentTerms = await _unitOfWork.Repository<StudentTerm>()
                    .Query()
                    .Where(st => st.TermId == request.TermId)
                    .ToListAsync(cancellationToken);

                foreach (var st in studentTerms)
                    await _unitOfWork.Repository<StudentTerm>().DeleteAsync(st, cancellationToken);
            }

            await _unitOfWork.SaveChangeAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            await _cacheService.RemoveByPatternAsync(TermCacheKeys.TermListPattern(), cancellationToken);
            await _cacheService.RemoveByPatternAsync(TermCacheKeys.TermDetailPattern(), cancellationToken);

            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Terms.LogTermDeleted), term.TermId, userId);

            var response = new DeleteTermResponse
            {
                Message = _messageService.GetMessage(MessageKeys.Terms.DeleteSuccess),
                HasRelatedData = studentTermsCount > 0,
                RelatedStudentTermsCount = studentTermsCount,
                RelatedInternshipGroupsCount = 0
            };

            return Result<DeleteTermResponse>.Success(response, response.Message);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Terms.LogErrorDeletingTerm), request.TermId);
            return Result<DeleteTermResponse>.Failure(
                _messageService.GetMessage(MessageKeys.Common.InternalError),
                ResultErrorType.InternalServerError);
        }
    }
}