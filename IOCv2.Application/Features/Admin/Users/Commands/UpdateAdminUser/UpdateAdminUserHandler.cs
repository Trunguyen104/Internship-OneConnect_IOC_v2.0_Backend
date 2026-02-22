using AutoMapper;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace IOCv2.Application.Features.Admin.Users.Commands.UpdateAdminUser
{
    public class UpdateAdminUserHandler : IRequestHandler<UpdateAdminUserCommand, Result<UpdateAdminUserResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMessageService _messageService;
        private readonly ICacheService _cacheService;

        public UpdateAdminUserHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService, IMessageService messageService, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUserService = currentUserService;
            _messageService = messageService;
            _cacheService = cacheService;
        }

        public async Task<Result<UpdateAdminUserResponse>> Handle(UpdateAdminUserCommand request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var auditorId))
            {
                return Result<UpdateAdminUserResponse>.Failure(
                    _messageService.GetMessage(MessageKeys.Users.InvalidAuditor),
                    ResultErrorType.Unauthorized
                );
            }

            var user = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken);

            if (user == null)
                return Result<UpdateAdminUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotFound));

            if (user.Status == UserStatus.Inactive)
                return Result<UpdateAdminUserResponse>.Failure(_messageService.GetMessage(MessageKeys.Users.NotActive));

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                // Update base fields
                user.FullName = request.FullName;

                if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                    user.PhoneNumber = request.PhoneNumber;

                if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
                    user.AvatarUrl = request.AvatarUrl;

                // Parse Status
                if (!string.IsNullOrWhiteSpace(request.Status) &&
                    Enum.TryParse<UserStatus>(request.Status, true, out var parsedStatus))
                {
                    user.Status = parsedStatus;
                }

                // Parse Gender
                if (!string.IsNullOrWhiteSpace(request.Gender) &&
                    Enum.TryParse<UserGender>(request.Gender, true, out var parsedGender))
                {
                    user.Gender = parsedGender;
                }

                // Parse DateOfBirth
                if (!string.IsNullOrWhiteSpace(request.DateOfBirth) &&
                    DateOnly.TryParse(request.DateOfBirth, out var parsedDob))
                {
                    user.DateOfBirth = parsedDob;
                }

                // Update Student fields if applicable
                if (user.Role == UserRole.Student && user.Student != null)
                {
                    if (request.StudentClass != null) user.Student.Class = request.StudentClass;
                    if (request.StudentMajor != null) user.Student.Major = request.StudentMajor;
                    if (request.StudentGpa != null) user.Student.Gpa = request.StudentGpa;
                    
                    await _unitOfWork.Repository<Student>().UpdateAsync(user.Student, cancellationToken);
                }

                await _unitOfWork.Repository<User>()
                    .UpdateAsync(user, cancellationToken);

                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                // Clear cache nếu có
                await _cacheService.RemoveAsync($"user:{user.UserId}");

                var response = _mapper.Map<UpdateAdminUserResponse>(user);

                return Result<UpdateAdminUserResponse>.Success(response);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }
    }
}
