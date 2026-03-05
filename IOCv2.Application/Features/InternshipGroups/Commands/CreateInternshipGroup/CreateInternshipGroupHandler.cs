using IOCv2.Application.Common.Models;
using IOCv2.Application.Interfaces;
using IOCv2.Domain.Entities;
using IOCv2.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using IOCv2.Application.Constants;
using AutoMapper;

namespace IOCv2.Application.Features.InternshipGroups.Commands.CreateInternshipGroup
{
    public class CreateInternshipGroupHandler : IRequestHandler<CreateInternshipGroupCommand, Result<CreateInternshipGroupResponse>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMessageService _messageService;
        private readonly IMapper _mapper;
        private readonly ILogger<CreateInternshipGroupHandler> _logger;

        public CreateInternshipGroupHandler(
            IUnitOfWork unitOfWork,
            IMessageService messageService,
            IMapper mapper,
            ILogger<CreateInternshipGroupHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _messageService = messageService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<Result<CreateInternshipGroupResponse>> Handle(CreateInternshipGroupCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogCreating), request.GroupName, request.TermId);

            try
            {
                // Validate TermId
                var termExists = await _unitOfWork.Repository<Term>()
                    .ExistsAsync(t => t.TermId == request.TermId, cancellationToken);
                if (!termExists)
                {
                    _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogTermNotFound), request.TermId);
                    return Result<CreateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.TermNotFound), ResultErrorType.NotFound);
                }

                // Validate EnterpriseId if provided
                if (request.EnterpriseId.HasValue)
                {
                    var enterpriseExists = await _unitOfWork.Repository<Enterprise>()
                        .ExistsAsync(e => e.EnterpriseId == request.EnterpriseId.Value, cancellationToken);
                    if (!enterpriseExists)
                    {
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogEnterpriseNotFound), request.EnterpriseId);
                        return Result<CreateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.EnterpriseNotFound), ResultErrorType.NotFound);
                    }
                }

                // Validate MentorId if provided
                if (request.MentorId.HasValue)
                {
                    var mentorExists = await _unitOfWork.Repository<EnterpriseUser>()
                        .ExistsAsync(u => u.EnterpriseUserId == request.MentorId.Value, cancellationToken);
                    if (!mentorExists)
                    {
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogMentorNotFound), request.MentorId);
                        return Result<CreateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.MentorNotFound), ResultErrorType.NotFound);
                    }
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);

                var newGroup = InternshipGroup.Create(
                    request.TermId,
                    request.GroupName,
                    request.EnterpriseId,
                    request.MentorId,
                    request.StartDate,
                    request.EndDate
                );

                // Nếu có gửi kèm danh sách sinh viên thì nạp luôn vào
                if (request.Students != null && request.Students.Any())
                {
                    var studentIds = request.Students.Select(s => s.StudentId).Distinct().ToList();

                    // Performance fix: Batch validation of students
                    var existingUsers = await _unitOfWork.Repository<Student>()
                        .FindAsync(u => studentIds.Contains(u.StudentId), cancellationToken);
                    var existingStudentIds = existingUsers.Select(u => u.StudentId).ToList();

                    if (existingStudentIds.Count != studentIds.Count)
                    {
                        var missingId = studentIds.Except(existingStudentIds).First();
                        _logger.LogWarning(_messageService.GetMessage(MessageKeys.InternshipGroups.LogStudentNotFound), missingId);
                        return Result<CreateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.InternshipGroups.StudentNotFound), ResultErrorType.NotFound);
                    }

                    foreach (var studentRef in request.Students)
                    {
                        var role = Enum.Parse<InternshipRole>(studentRef.Role, ignoreCase: true);
                        newGroup.AddMember(studentRef.StudentId, role);
                    }
                }

                await _unitOfWork.Repository<InternshipGroup>().AddAsync(newGroup);
                var saved = await _unitOfWork.SaveChangeAsync(cancellationToken);

                if (saved > 0)
                {
                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                    _logger.LogInformation(_messageService.GetMessage(MessageKeys.InternshipGroups.LogCreatedSuccess), newGroup.InternshipId);

                    var response = _mapper.Map<CreateInternshipGroupResponse>(newGroup);
                    return Result<CreateInternshipGroupResponse>.Success(response);
                }

                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(_messageService.GetMessage(MessageKeys.InternshipGroups.LogCreationFailed));
                return Result<CreateInternshipGroupResponse>.Failure(_messageService.GetMessage(MessageKeys.Common.DatabaseUpdateError));
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, _messageService.GetMessage(MessageKeys.InternshipGroups.LogCreationError));
                throw; // Let ExceptionMiddleware handle it
            }
        }
    }
}
