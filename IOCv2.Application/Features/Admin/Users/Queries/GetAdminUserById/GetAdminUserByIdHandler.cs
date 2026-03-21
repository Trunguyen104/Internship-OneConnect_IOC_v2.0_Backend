using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Features.Admin.Users.Common;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Admin.Users.Queries.GetAdminUserById
{
    public class GetAdminUserByIdHandler : IRequestHandler<GetAdminUserByIdQuery, Result<GetAdminUserByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<GetAdminUserByIdHandler> _logger;

        public GetAdminUserByIdHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMessageService messageService, 
            ICacheService cacheService,
            ILogger<GetAdminUserByIdHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<Result<GetAdminUserByIdResponse>> Handle(GetAdminUserByIdQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting admin user by id {UserId}", request.UserId);

            var cacheKey = AdminUserCacheKeys.User(request.UserId);
            var cachedUser = await _cacheService.GetAsync<GetAdminUserByIdResponse>(cacheKey, cancellationToken);
            if (cachedUser != null)
            {
                _logger.LogInformation("Admin user {UserId} loaded from cache", request.UserId);
                return Result<GetAdminUserByIdResponse>.Success(cachedUser);
            }

            var user = await _unitOfWork.Repository<User>().Query()
                .Include(u => u.Student)
                .Include(u => u.UniversityUser).ThenInclude(uu => uu!.University)
                .Include(u => u.EnterpriseUser).ThenInclude(eu => eu!.Enterprise)
                .AsNoTracking()
                .ProjectTo<GetAdminUserByIdResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Admin user {UserId} not found", request.UserId);
                return Result<GetAdminUserByIdResponse>.NotFound(_messageService.GetMessage(MessageKeys.Users.NotFound));
            }

            await _cacheService.SetAsync(cacheKey, user, AdminUserCacheKeys.Expiration.User, cancellationToken);

            _logger.LogInformation("Successfully retrieved admin user {UserId}", request.UserId);

            return Result<GetAdminUserByIdResponse>.Success(user);
        }
    }
}
