using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;

namespace IOCv2.Application.Features.Users.Queries.GetDownloadMyCV
{
    public class GetDownloadMyCVHandler : IRequestHandler<GetDownloadMyCVQuery, Result<GetDownloadMyCVResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<GetDownloadMyCVHandler> _logger;

        public GetDownloadMyCVHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IFileStorageService fileStorageService,
            ILogger<GetDownloadMyCVHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        public async Task<Result<GetDownloadMyCVResponse>> Handle(GetDownloadMyCVQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return Result<GetDownloadMyCVResponse>.Failure("Common.Unauthorized", ResultErrorType.Unauthorized);
            }

            var student = await _unitOfWork.Repository<Student>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

            if (student == null)
            {
                return Result<GetDownloadMyCVResponse>.Failure("Student profile not found.", ResultErrorType.NotFound);
            }

            if (string.IsNullOrEmpty(student.CvUrl))
            {
                return Result<GetDownloadMyCVResponse>.Failure("CV not found for this profile.", ResultErrorType.NotFound);
            }

            // Get stream from storage
            var stream = await _fileStorageService.GetFileAsync(student.CvUrl, cancellationToken);
            if (stream == null)
            {
                _logger.LogWarning("CV file not found in storage for User: {UserId}", userId);
                return Result<GetDownloadMyCVResponse>.Failure("File not found in storage.", ResultErrorType.NotFound);
            }

            var extension = Path.GetExtension(student.CvUrl);
            var safeFileName = $"CV_{_currentUserService.UserId.Substring(0, 8)}{extension}";

            var response = new GetDownloadMyCVResponse
            {
                Content = stream,
                ContentType = FileValidationHelper.GetMimeType(safeFileName),
                FileName = safeFileName
            };

            return Result<GetDownloadMyCVResponse>.Success(response);
        }
    }
}
