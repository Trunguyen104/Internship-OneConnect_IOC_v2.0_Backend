using AutoMapper;
using AutoMapper.QueryableExtensions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Users.Queries.GetMyProfile
{
    public class GetMyProfileHandler : IRequestHandler<GetMyProfileQuery, Result<GetMyProfileResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;
        private readonly ILogger<GetMyProfileHandler> _logger;

        public GetMyProfileHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService,
            ILogger<GetMyProfileHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
            _logger = logger;
        }

        public async Task<Result<GetMyProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Getting profile for User {UserId}", request.UserId);


            var user = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.Student)
                .Include(u => u.UniversityUser)
                .Include(u => u.EnterpriseUser)
                .AsNoTracking()
                .ProjectTo<GetMyProfileResponse>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found when fetching profile", request.UserId);
                return Result<GetMyProfileResponse>.NotFound(_messageService.GetMessage(MessageKeys.Users.NotFound));
            }

            return Result<GetMyProfileResponse>.Success(user);

        }
    }
}
