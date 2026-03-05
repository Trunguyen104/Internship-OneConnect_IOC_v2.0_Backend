using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Constants;
using AutoMapper;

namespace IOCv2.Application.Features.InternshipGroups.Commands.AddStudentsToGroup
{
    public class AddStudentsToGroupHandler : IRequestHandler<AddStudentsToGroupCommand, Result<AddStudentsToGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<AddStudentsToGroupHandler> _logger;

        public AddStudentsToGroupHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ILogger<AddStudentsToGroupHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<AddStudentsToGroupResponse>> Handle(AddStudentsToGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogAddingStudents), request.InternshipId);

            try
            {
                var group = await _unitOfWork.Repository<InternshipGroup>().Query()
                    .Include(g => g.Members)
                    .FirstOrDefaultAsync(x => x.InternshipId == request.InternshipId, cancellationToken);

                if (group == null)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogNotFound), request.InternshipId);
                    return Result<AddStudentsToGroupResponse>.NotFound(_messageService.GetMessage(MessageKeys.Common.NotFound));
                }

                var studentIds = request.Students.Select(s => s.StudentId).Distinct().ToList();

                // Validate students exist in system
                var existingUsers = await _unitOfWork.Repository<Student>().FindAsync(s => studentIds.Contains(s.StudentId), cancellationToken);
                var existingStudentIds = existingUsers.Select(s => s.StudentId).ToList();

                if (existingStudentIds.Count != studentIds.Count)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogInvalidStudentIds));
                    return Result<AddStudentsToGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.InvalidStudentId));
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                foreach (var item in request.Students)
                {
                    var role = Enum.Parse<InternshipRole>(item.Role, ignoreCase: true);
                    group.AddMember(item.StudentId, role);
                }

                await _unitOfWork.Repository<InternshipGroup>().UpdateAsync(group);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved >= 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogAddedStudentsSuccess), studentIds.Count, request.InternshipId);

                    var response = _mapper.Map<AddStudentsToGroupResponse>(group);
                    return Result<AddStudentsToGroupResponse>.Success(response);
                }

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(_messageService.GetMessage(MessageKeys.InternshipGroups.LogAddStudentsFailed));
                return Result<AddStudentsToGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogAddStudentsError));
                throw;
            }
        }
    }
}
