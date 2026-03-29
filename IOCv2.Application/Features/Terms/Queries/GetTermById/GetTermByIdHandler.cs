using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Terms.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
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
    private readonly ICacheService _cacheService;

    public GetTermByIdHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IMessageService messageService,
        ILogger<GetTermByIdHandler> logger,
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

    public async Task<Result<GetTermByIdResponse>> Handle(GetTermByIdQuery request, CancellationToken cancellationToken)
    {
      
            var userId = Guid.Parse(_currentUserService.UserId!);
            var role = _currentUserService.Role ?? string.Empty;
            var isSuperAdmin = string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            var isEnterpriseScopedRole =
                string.Equals(role, "EnterpriseAdmin", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "HR", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(role, "Mentor", StringComparison.OrdinalIgnoreCase);

            IQueryable<Term> termQuery;
            string cacheKey;

            if (isSuperAdmin)
            {
                // SuperAdmin: access any term regardless of university
                cacheKey = TermCacheKeys.Term(request.TermId);
                var cached = await _cacheService.GetAsync<GetTermByIdResponse>(cacheKey, cancellationToken);
                if (cached is not null)
                    return Result<GetTermByIdResponse>.Success(cached);

                termQuery = _unitOfWork.Repository<Term>()
                    .Query()
                    .Where(t => t.TermId == request.TermId);
            }
            else if (isEnterpriseScopedRole)
            {
                // EnterpriseAdmin / HR / Mentor: can only view terms linked to their enterprise
                var enterpriseUser = await _unitOfWork.Repository<EnterpriseUser>()
                    .Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(eu => eu.UserId == userId, cancellationToken);

                if (enterpriseUser == null)
                    return Result<GetTermByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);

                var eid = enterpriseUser.EnterpriseId;

                cacheKey = TermCacheKeys.Term(request.TermId) + $":enterprise:{eid}";
                var cached = await _cacheService.GetAsync<GetTermByIdResponse>(cacheKey, cancellationToken);
                if (cached is not null)
                    return Result<GetTermByIdResponse>.Success(cached);

                // Verify the term is accessible to this enterprise
                var termInScope = await _unitOfWork.Repository<Term>()
                    .Query()
                    .AsNoTracking()
                    .Where(t => t.TermId == request.TermId)
                    .Where(t =>
                        _unitOfWork.Repository<InternshipApplication>().Query()
                            .Any(a => a.TermId == t.TermId && a.EnterpriseId == eid) ||
                        _unitOfWork.Repository<StudentTerm>().Query()
                            .Any(st => st.TermId == t.TermId && st.EnterpriseId == eid && st.EnrollmentStatus == EnrollmentStatus.Active))
                    .AnyAsync(cancellationToken);

                if (!termInScope)
                    return Result<GetTermByIdResponse>.Failure(
                        _messageService.GetMessage(MessageKeys.Terms.NotFound),
                        ResultErrorType.NotFound);

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

                cacheKey = TermCacheKeys.Term(request.TermId, universityUser.UniversityId);
                var cached = await _cacheService.GetAsync<GetTermByIdResponse>(cacheKey, cancellationToken);
                if (cached is not null)
                    return Result<GetTermByIdResponse>.Success(cached);

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

            await _cacheService.SetAsync(cacheKey, term, TermCacheKeys.Expiration.Term, cancellationToken);

            return Result<GetTermByIdResponse>.Success(term);
    
    }
}