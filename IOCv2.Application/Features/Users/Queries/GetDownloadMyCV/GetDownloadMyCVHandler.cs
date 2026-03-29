using IOCv2.Application.Common.Helpers;
using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using IOCv2.Application.Constants;

namespace IOCv2.Application.Features.Users.Queries.GetDownloadMyCV
{
    public class GetDownloadMyCVHandler : IRequestHandler<GetDownloadMyCVQuery, Result<GetDownloadMyCVResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUserService _currentUserService;
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<GetDownloadMyCVHandler> _logger;
        private readonly IMessageService _messageService;

        public GetDownloadMyCVHandler(
            IUnitOfWork unitOfWork,
            ICurrentUserService currentUserService,
            IFileStorageService fileStorageService,
            ILogger<GetDownloadMyCVHandler> logger,
            IMessageService messageService)
        {
            _unitOfWork = unitOfWork;
            _currentUserService = currentUserService;
            _fileStorageService = fileStorageService;
            _logger = logger;
            _messageService = messageService;
        }

        public async Task<Result<GetDownloadMyCVResponse>> Handle(GetDownloadMyCVQuery request, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(_currentUserService.UserId, out var userId))
            {
                return Result<GetDownloadMyCVResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);
            }

            var student = await _unitOfWork.Repository<Student>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.UserId == userId, cancellationToken);

            if (student == null)
            {
                return Result<GetDownloadMyCVResponse>.Failure(_messageService.GetMessage(MessageKeys.Profile.StudentNotFound), ResultErrorType.NotFound);
            }

            if (string.IsNullOrEmpty(student.CvUrl))
            {
                return Result<GetDownloadMyCVResponse>.Failure(_messageService.GetMessage(MessageKeys.Profile.CvNotFound), ResultErrorType.NotFound);
            }

            // Get stream from storage
            var stream = await _fileStorageService.GetFileAsync(student.CvUrl, cancellationToken);
            if (stream == null)
            {
                _logger.LogWarning(_messageService.GetMessage(MessageKeys.Profile.LogCvFileNotFound), userId);
                return Result<GetDownloadMyCVResponse>.Failure(_messageService.GetMessage(MessageKeys.Profile.FileNotFoundInStorage), ResultErrorType.NotFound);
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
