using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Admin.Users.Queries.GetAdminUserById
{
    public class GetAdminUserByIdHandler : IRequestHandler<GetAdminUserByIdQuery, Result<GetAdminUserByIdResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public GetAdminUserByIdHandler(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IMessageService messageService, 
            ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<GetAdminUserByIdResponse>> Handle(GetAdminUserByIdQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = $"user:{request.UserId}";
            var cachedUser = await _cacheService.GetAsync<GetAdminUserByIdResponse>(cacheKey, cancellationToken);
            if (cachedUser != null)
            {
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
                return Result<GetAdminUserByIdResponse>.NotFound(_messageService.GetMessage(MessageKeys.Users.NotFound));
            }

            await _cacheService.SetAsync(cacheKey, user, cancellationToken: cancellationToken);

            return Result<GetAdminUserByIdResponse>.Success(user);
        }
    }
}
