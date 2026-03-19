using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.ProjectResources.Common;
using IOCv2.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.ProjectResources.Queries.GetProjectResources.GetAllProjectResources
{
    public class GetAllProjectResourcesHandler : IRequestHandler<GetAllProjectResourcesQuery, Result<PaginatedResult<GetAllProjectResourcesResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAllProjectResourcesHandler> _logger;
        private readonly IMessageService _messageService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICacheService _cacheService;

        public GetAllProjectResourcesHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<GetAllProjectResourcesHandler> logger,
            IMessageService messageService,
            ICurrentUserService currentUserService,
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _messageService = messageService;
            _currentUserService = currentUserService;
            _cacheService = cacheService;
        }

        public async Task<Result<PaginatedResult<GetAllProjectResourcesResponse>>> Handle(
            GetAllProjectResourcesQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = ProjectResourceCacheKeys.List(
                    request.ProjectId,
                    request.PageNumber,
                    request.PageSize,
                    request.SortColumn,
                    request.SortOrder,
                    request.ResourceType.HasValue ? (int)request.ResourceType.Value : null,
                    request.SearchTerm,
                    _currentUserService.UserId);

                var cached = await _cacheService.GetAsync<PaginatedResult<GetAllProjectResourcesResponse>>(cacheKey, cancellationToken);
                if (cached != null)
                {
                    return Result<PaginatedResult<GetAllProjectResourcesResponse>>.Success(cached);
                }

                if (string.IsNullOrWhiteSpace(_currentUserService.UserId) ||
                    !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
                {
                    return Result<PaginatedResult<GetAllProjectResourcesResponse>>.Failure(
                        _messageService.GetMessage(MessageKeys.Common.Forbidden),
                        ResultErrorType.Forbidden);
                }

                var query = _unitOfWork.Repository<Domain.Entities.ProjectResources>().Query().AsNoTracking();

                var isAdmin = string.Equals(_currentUserService.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(_currentUserService.Role, "Admin", StringComparison.OrdinalIgnoreCase);

                if (!isAdmin)
                {
                    var studentId = await _unitOfWork.Repository<Domain.Entities.Student>().Query()
                        .AsNoTracking()
                        .Where(s => s.UserId == currentUserId)
                        .Select(s => s.StudentId)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (studentId == Guid.Empty)
                    {
                        return Result<PaginatedResult<GetAllProjectResourcesResponse>>.Failure(
                            _messageService.GetMessage(MessageKeys.Common.Forbidden),
                            ResultErrorType.Forbidden);
                    }

                    query = from resource in query
                            join project in _unitOfWork.Repository<Domain.Entities.Project>().Query().AsNoTracking()
                                on resource.ProjectId equals project.ProjectId
                            join member in _unitOfWork.Repository<Domain.Entities.InternshipStudent>().Query().AsNoTracking()
                                on project.InternshipId equals member.InternshipId
                            where member.StudentId == studentId
                            select resource;
                }

                if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                {
                    var term = request.SearchTerm.Trim().ToLowerInvariant();
                    query = query.Where(x => x.ResourceName != null && x.ResourceName.ToLower().Contains(term));
                }

                if (request.ProjectId.HasValue)
                {
                    query = query.Where(x => x.ProjectId == request.ProjectId.Value);
                }

                if (request.ResourceType.HasValue)
                {
                    query = query.Where(x => x.ResourceType == request.ResourceType.Value);
                }

                var totalCount = await query.CountAsync(cancellationToken);

                query = ApplySorting(query, request.SortColumn, request.SortOrder);

                var items = await query
                    .Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ProjectTo<GetAllProjectResourcesResponse>(_mapper.ConfigurationProvider)
                    .ToListAsync(cancellationToken);

                var result = PaginatedResult<GetAllProjectResourcesResponse>.Create(
                    items, totalCount, request.PageNumber, request.PageSize);

                _logger.LogInformation(_messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetAllSuccess), items.Count);

                await _cacheService.SetAsync(cacheKey, result, ProjectResourceCacheKeys.Expiration.List, cancellationToken);

                return Result<PaginatedResult<GetAllProjectResourcesResponse>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.ProjectResourcesKey.GetAllError));
                return Result<PaginatedResult<GetAllProjectResourcesResponse>>.Failure(
                    _messageService.GetMessage(MessageKeys.Common.InternalError),
                    ResultErrorType.InternalServerError);
            }
        }

        private IQueryable<Domain.Entities.ProjectResources> ApplySorting(
            IQueryable<Domain.Entities.ProjectResources> query,
            string? sortColumn,
            string? sortOrder)
        {
            var isDescending = sortOrder?.ToLower() == "desc";

            return (sortColumn?.ToLower(), isDescending) switch
            {
                ("resourcename", true) => query.OrderByDescending(x => x.ResourceName),
                ("resourcename", false) => query.OrderBy(x => x.ResourceName),

                ("resourcetype", true) => query.OrderByDescending(x => x.ResourceType),
                ("resourcetype", false) => query.OrderBy(x => x.ResourceType),

                ("createdat", true) => query.OrderByDescending(x => x.CreatedAt),
                ("createdat", false) => query.OrderBy(x => x.CreatedAt),

                _ => query.OrderByDescending(x => x.CreatedAt)
            };
        }
    }
}
