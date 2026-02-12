using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IOCv2.Application.Features.Users.Queries.GetMyProfile
{
    public class GetMyProfileHandler : IRequestHandler<GetMyProfileQuery, Result<GetMyProfileResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IMessageService _messageService;

        public GetMyProfileHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _messageService = messageService;
        }

        public async Task<Result<GetMyProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
        {
            var user = await _unitOfWork.Repository<User>()
                .Query()
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (user == null)
            {
                return Result<GetMyProfileResponse>.NotFound(_messageService.GetMessage(MessageKeys.Users.NotFound));
            }

            var response = _mapper.Map<GetMyProfileResponse>(user);
            return Result<GetMyProfileResponse>.Success(response);
        }
    }
}
