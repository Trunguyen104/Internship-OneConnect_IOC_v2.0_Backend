using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Users.Commands.UpdateMyProfile
{
    public class UpdateMyProfileHandler : IRequestHandler<UpdateMyProfileCommand, Result<Unit>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<UpdateMyProfileHandler> _logger;

        public UpdateMyProfileHandler(
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ILogger<UpdateMyProfileHandler> logger)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<Result<Unit>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("[START] UpdateMyProfile for User: {UserId}", _currentUserService.UserId);

            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                _logger.LogWarning("[AUTH_FAILED] User ID not found in context.");
                return Result<Unit>.Failure("Common.Unauthorized", ResultErrorType.Unauthorized);
            }

            var user = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.Student)
                .Include(u => u.EnterpriseUser)
                .Include(u => u.UniversityUser)
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("[NOT_FOUND] User not found: {UserId}", userId);
                throw new NotFoundException(nameof(User), userId);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            // FFA-CAG: Business logic within Entity method
            user.UpdateProfile(
                request.FullName,
                request.PhoneNumber,
                request.AvatarUrl,
                request.Gender,
                request.DateOfBirth);

            // FFA-FLW: Role-based Metadata Handling
            UpdateMetadata(user, request);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                _logger.LogInformation("[SUCCESS] Profile updated for User: {UserId}", userId);
                return Result<Unit>.Success(Unit.Value);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "[ERROR] Failed to update profile for User: {UserId}", userId);
                return Result<Unit>.Failure("Common.InternalError", ResultErrorType.InternalServerError);
            }
        }

        private void UpdateMetadata(User user, UpdateMyProfileCommand request)
        {
            switch (user.Role)
            {
                case UserRole.Student when user.Student != null:
                    user.Student.UpdatePortfolio(request.PortfolioUrl);
                    break;

                case UserRole.Mentor:
                case UserRole.HR:
                case UserRole.EnterpriseAdmin:
                    if (user.EnterpriseUser != null)
                    {
                        user.EnterpriseUser.UpdateMetadata(request.Bio, request.Expertise);
                    }
                    break;

                case UserRole.SchoolAdmin:
                    if (user.UniversityUser != null)
                    {
                        user.UniversityUser.UpdateMetadata(request.Bio, request.Department);
                    }
                    break;
            }
        }
    }
}
