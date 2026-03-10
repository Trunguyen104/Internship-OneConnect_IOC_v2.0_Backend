using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Terms.Queries.GetTermById;

public class GetTermByIdHandler : IRequestHandler<GetTermByIdQuery, Result<GetTermByIdResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetTermByIdHandler> _logger;
    private readonly IMapper _mapper;
    private readonly IMessageService _messageService;
    private readonly IUnitOfWork _unitOfWork;

    public GetTermByIdHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ILogger<GetTermByIdHandler> logger,
        ICurrentUserService currentUserService)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _messageService = messageService;
        _logger = logger;
        _currentUserService = currentUserService;
    }

    public async Task<Result<GetTermByIdResponse>> Handle(GetTermByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(_currentUserService.UserId!);
            var isSuperAdmin =
                string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

            IQueryable<Term> termQuery;

            if (isSuperAdmin)
            {
                // SuperAdmin: access any term regardless of university
                termQuery = _unitOfWork.Repository<Term>()
                    .Query()
                    .Where(t => t.TermId == request.TermId);
            }
            else
            {
                // SchoolAdmin: resolve university then restrict to their own
                var universityUser = await _unitOfWork.Repository<UniversityUser>()
                    .Query()
                    .Where(uu => uu.UserId == userId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (universityUser == null)
                    return Result<GetTermByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.University.NotFound),
                        ResultErrorType.NotFound);

                termQuery = _unitOfWork.Repository<Term>()
                    .Query()
                    .Where(t => t.TermId == request.TermId && t.UniversityId == universityUser.UniversityId);
            }

            var term = await termQuery
                .AsNoTracking()
                .ProjectTo<GetTermByIdResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (term == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Terms.LogTermNotFoundOrAccessDenied),
                    request.TermId, userId);
                return Result<GetTermByIdResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Terms.NotFound),
                    ResultErrorType.NotFound);
            }

            return Result<GetTermByIdResponse>.Success(term);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Terms.LogErrorRetrievingTerm), request.TermId);
            throw;
        }
    }
}