using IOCv2.Application.Common.Models;
using IOCv2.Application.Constants;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IOCv2.Application.Features.Projects.Queries.GetProjectStudents
{
    public class GetProjectStudentsHandler : IRequestHandler<GetProjectStudentsQuery, Result<List<GetProjectStudentsResponse>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _message;
        private readonly ILogger<GetProjectStudentsHandler> _logger;
        private readonly ICurrentUserService? _currentUserService;

        public GetProjectStudentsHandler(
            IUnitOfWork unitOfWork,
            IMessageService message,
            ILogger<GetProjectStudentsHandler> logger,
            ICurrentUserService? currentUserService = null)
        {
            _unitOfWork = unitOfWork;
            _message = message;
            _logger = logger;
            _currentUserService = currentUserService;
        }

        public async Task<Result<List<GetProjectStudentsResponse>>> Handle(
            GetProjectStudentsQuery request,
            CancellationToken cancellationToken)
        {
            var project = await _unitOfWork.Repository<Project>().Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, cancellationToken);

            if (project == null)
                return Result<List<GetProjectStudentsResponse>>.Failure(
                    _message.GetMessage(MessageKeys.Projects.NotFound), ResultErrorType.NotFound);

            if (project.InternshipId == null)
            {
                _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogGetStudentsSuccess), request.ProjectId);
                return Result<List<GetProjectStudentsResponse>>.Success(new List<GetProjectStudentsResponse>());
            }

            var isStudent = string.Equals(_currentUserService?.Role, "Student", StringComparison.OrdinalIgnoreCase);
            if (isStudent)
            {
                if (project.Status != ProjectStatus.Published && project.Status != ProjectStatus.Completed)
                    return Result<List<GetProjectStudentsResponse>>.Failure(
                        _message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                if (!Guid.TryParse(_currentUserService?.UserId, out var currentUserId))
                    return Result<List<GetProjectStudentsResponse>>.Failure(
                        _message.GetMessage(MessageKeys.Common.Unauthorized), ResultErrorType.Unauthorized);

                var studentId = await _unitOfWork.Repository<Student>().Query()
                    .AsNoTracking()
                    .Where(s => s.UserId == currentUserId)
                    .Select(s => s.StudentId)
                    .FirstOrDefaultAsync(cancellationToken);

                if (studentId == Guid.Empty)
                    return Result<List<GetProjectStudentsResponse>>.Failure(
                        _message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);

                var isMember = await _unitOfWork.Repository<InternshipStudent>().Query()
                    .AsNoTracking()
                    .AnyAsync(s => s.InternshipId == project.InternshipId.Value && s.StudentId == studentId, cancellationToken);

                if (!isMember)
                    return Result<List<GetProjectStudentsResponse>>.Failure(
                        _message.GetMessage(MessageKeys.Common.Forbidden), ResultErrorType.Forbidden);
            }

            var students = await _unitOfWork.Repository<InternshipStudent>().Query()
                .Where(s => s.InternshipId == project.InternshipId.Value)
                .Include(s => s.Student)
                    .ThenInclude(st => st.User)
                .AsNoTracking()
                .Select(s => new GetProjectStudentsResponse
                {
                    StudentId = s.StudentId,
                    FullName = s.Student.User.FullName,
                    Email = s.Student.User.Email,
                    ClassName = s.Student.ClassName
                })
                .ToListAsync(cancellationToken);

            _logger.LogInformation(_message.GetMessage(MessageKeys.Projects.LogGetStudentsSuccess), request.ProjectId);
            return Result<List<GetProjectStudentsResponse>>.Success(students);
        }
    }
}

