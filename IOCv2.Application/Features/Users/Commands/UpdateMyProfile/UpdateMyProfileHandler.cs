using IOCv2.Application.Common.Exceptions;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Features.Admin.UserManagement.Common;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Users.Commands.UpdateMyProfile
{
    public class UpdateMyProfileHandler : IRequestHandler<UpdateMyProfileCommand, Result<Unit>>
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<UpdateMyProfileHandler> _logger;
        private readonly IMessageService _messageService;

        public UpdateMyProfileHandler(
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IFileStorageService fileStorageService,
            ILogger<UpdateMyProfileHandler> logger,
            IMessageService messageService)
        {
            _currentUserService = currentUserService;
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _fileStorageService = fileStorageService;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<Unit>> Handle(UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.Profile.LogUpdateStart), _currentUserService.UserId);

            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Profile.LogAuthFailed));
                return Result<Unit>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            var user = await _unitOfWork.Repository<User>()
                .Query()
                .Include(u => u.Student)
                .Include(u => u.EnterpriseUser)
                .Include(u => u.UniversityUser)
                .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Profile.LogUserNotFound), userId);
                throw new NotFoundException(nameof(User), userId);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            // FFA-CAG: Business logic within Entity method
            user.UpdateProfile(
                request.FullName,
                request.PhoneNumber,
                request.AvatarUrl,
                request.Gender,
                request.DateOfBirth,
                request.Address);

            // FFA-FLW: Role-based Metadata Handling
            await UpdateMetadataAsync(user, request, cancellationToken);

            try
            {
                await _unitOfWork.SaveChangeAsync(cancellationToken);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                
                // FFA-LOG: Cache invalidation
                await _cacheService.RemoveAsync(UserManagementCacheKeys.User(userId), cancellationToken);
                await _cacheService.RemoveByPatternAsync(UserManagementCacheKeys.UserListPattern(), cancellationToken);

                _logger.LogInformation(_messageService.GetMessage(MessageKeys.Profile.LogUpdateSuccess), userId);
                return Result<Unit>.Success(Unit.Value, _messageService.GetMessage(MessageKeys.Profile.UpdateSuccess));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.Profile.LogUpdateError), userId);
                return Result<Unit>.Failure(_messageService.GetMessage(MessageKeys.Common.InternalError), ResultErrorType.InternalServerError);
            }
        }

        private async Task UpdateMetadataAsync(User user, UpdateMyProfileCommand request, CancellationToken cancellationToken)
        {
            switch (user.Role)
            {
                case UserRole.Student when user.Student != null:
                    if (request.PortfolioUrl != null)
                    {
                        user.Student.UpdatePortfolio(string.IsNullOrWhiteSpace(request.PortfolioUrl) ? null : request.PortfolioUrl);
                    }
                    
                    // Handle CV File Upload
                    if (request.CvFile != null)
                    {
                        var cvUrl = await _fileStorageService.UploadFileAsync(request.CvFile, "CVs", cancellationToken: cancellationToken);
                        user.Student.UpdateCv(cvUrl);
                    }
                    else if (request.CvUrl != null)
                    {
                        user.Student.UpdateCv(string.IsNullOrWhiteSpace(request.CvUrl) ? null : request.CvUrl);
                    }
                    
                    if (!string.IsNullOrWhiteSpace(request.Major)) user.Student.Major = request.Major;
                    if (!string.IsNullOrWhiteSpace(request.ClassName)) user.Student.ClassName = request.ClassName;
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
