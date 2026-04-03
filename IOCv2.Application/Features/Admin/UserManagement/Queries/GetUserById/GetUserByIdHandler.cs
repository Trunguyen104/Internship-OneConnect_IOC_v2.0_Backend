using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Features.Admin.UserManagement.Common;

namespace IOCv2.Application.Features.Admin.UserManagement.Queries.GetUserById
{
    public class GetUserByIdHandler : IRequestHandler<GetUserByIdQuery, Result<GetUserByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<GetUserByIdHandler> _logger;

        public GetUserByIdHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMessageService messageService, 
            ICacheService cacheService,
            ICurrentUserService currentUserService,
            ILogger<GetUserByIdHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _cacheService = cacheService;
            _currentUserService = currentUserService;
            _logger = logger;
        }

        public async Task<Result<GetUserByIdResponse>> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting user by id {UserId} requested by {AuditorId}", request.UserId, _currentUserService.UserId);

            var auditorRoleStr = _currentUserService.Role;
            var auditorUnitId = _currentUserService.UnitId;
            
            if (!Enum.TryParse<UserRole>(auditorRoleStr, true, out var auditorRole))
            {
                return Result<GetUserByIdResponse>.Failure("Invalid auditor role", ResultErrorType.Forbidden);
            }

            var cacheKey = UserManagementCacheKeys.User(request.UserId);
            var cachedUser = await _cacheService.GetAsync<GetUserByIdResponse>(cacheKey, cancellationToken);
            if (cachedUser != null)
            {
                return Result<GetUserByIdResponse>.Success(cachedUser);
            }

            var query = _unitOfWork.Repository<User>().Query()
                .Include(u => u.Student)
                .Include(u => u.UniversityUser).ThenInclude(uu => uu!.University)
                .Include(u => u.EnterpriseUser).ThenInclude(eu => eu!.Enterprise)
                .AsNoTracking();

            var userEntity = await query.FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);
            if (userEntity == null)
            {
                return Result<GetUserByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound), ResultErrorType.NotFound);
            }

            // Hierarchical Scoping Rules
            if (auditorRole == UserRole.SchoolAdmin)
            {
                if (userEntity.Role != UserRole.Student || userEntity.UniversityUser?.UniversityId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: SchoolAdmin attempted to access user {UserId}", userEntity.UserId);
                    return Result<GetUserByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }
            }
            else if (auditorRole == UserRole.EnterpriseAdmin)
            {
                if ((userEntity.Role != UserRole.HR && userEntity.Role != UserRole.Mentor) || userEntity.EnterpriseUser?.EnterpriseId.ToString() != auditorUnitId)
                {
                    _logger.LogWarning("Access Denied: EnterpriseAdmin attempted to access user {UserId}", userEntity.UserId);
                    return Result<GetUserByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
                }
            }
            else if (auditorRole != UserRole.SuperAdmin && auditorRole != UserRole.Moderator)
            {
                return Result<GetUserByIdResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.AccessDenied), ResultErrorType.Forbidden);
            }

            var response = _mapper.Map<GetUserByIdResponse>(userEntity);

            await _cacheService.SetAsync(cacheKey, response, UserManagementCacheKeys.Expiration.User, cancellationToken);

            return Result<GetUserByIdResponse>.Success(response);
        }
    }
}
